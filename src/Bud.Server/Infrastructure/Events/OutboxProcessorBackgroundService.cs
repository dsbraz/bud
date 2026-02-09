using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bud.Server.Infrastructure.Events;

public sealed partial class OutboxProcessorBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxProcessorBackgroundService> logger,
    IOptions<OutboxProcessingOptions> processingOptions) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<OutboxEventProcessor>();
                var batchSize = Math.Max(1, processingOptions.Value.BatchSize);
                var processedCount = await processor.ProcessPendingAsync(batchSize, stoppingToken);
                LogOutboxBatchProcessed(logger, processedCount, batchSize);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                LogOutboxProcessingLoopFailed(logger, ex);
            }

            var pollingInterval = processingOptions.Value.PollingInterval <= TimeSpan.Zero
                ? TimeSpan.FromSeconds(5)
                : processingOptions.Value.PollingInterval;
            await Task.Delay(pollingInterval, stoppingToken);
        }
    }

    [LoggerMessage(
        EventId = 3200,
        Level = LogLevel.Debug,
        Message = "Ciclo de processamento do outbox concluído. ProcessedCount={ProcessedCount} BatchSize={BatchSize}")]
    private static partial void LogOutboxBatchProcessed(ILogger logger, int processedCount, int batchSize);

    [LoggerMessage(
        EventId = 3201,
        Level = LogLevel.Error,
        Message = "Falha no loop de processamento do outbox.")]
    private static partial void LogOutboxProcessingLoopFailed(ILogger logger, Exception exception);
}
