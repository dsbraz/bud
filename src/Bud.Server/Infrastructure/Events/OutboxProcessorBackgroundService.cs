using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Bud.Server.Logging;
using Microsoft.Extensions.Options;

namespace Bud.Server.Infrastructure.Events;

public sealed class OutboxProcessorBackgroundService(
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
                await processor.ProcessPendingAsync(batchSize, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogOutboxProcessingFailed(ex);
            }

            var pollingInterval = processingOptions.Value.PollingInterval <= TimeSpan.Zero
                ? TimeSpan.FromSeconds(5)
                : processingOptions.Value.PollingInterval;
            await Task.Delay(pollingInterval, stoppingToken);
        }
    }
}
