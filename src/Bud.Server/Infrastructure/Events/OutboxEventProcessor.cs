using System.Reflection;
using Bud.Server.Application.Common.Events;
using Bud.Server.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bud.Server.Infrastructure.Events;

public sealed partial class OutboxEventProcessor(
    ApplicationDbContext dbContext,
    IServiceProvider serviceProvider,
    IOutboxEventSerializer serializer,
    IOptions<OutboxProcessingOptions> processingOptions,
    ILogger<OutboxEventProcessor> logger,
    Func<DateTime>? utcNowProvider = null)
{
    public async Task<int> ProcessPendingAsync(int batchSize = 100, CancellationToken cancellationToken = default)
    {
        var nowProvider = utcNowProvider ?? (() => DateTime.UtcNow);
        var now = nowProvider();
        var options = processingOptions.Value;
        var safeBatchSize = Math.Max(1, batchSize);

        var messages = await dbContext.OutboxMessages
            .Where(m => m.ProcessedOnUtc == null &&
                        m.DeadLetteredOnUtc == null &&
                        (m.NextAttemptOnUtc == null || m.NextAttemptOnUtc <= now))
            .OrderBy(m => m.OccurredOnUtc)
            .Take(safeBatchSize)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            var startedAt = nowProvider();
            LogOutboxMessageProcessingStarted(logger, message.Id, message.EventType, message.RetryCount + 1);

            try
            {
                var domainEvent = serializer.Deserialize(message.EventType, message.Payload);
                await DispatchToSubscribersAsync(domainEvent, cancellationToken);
                message.ProcessedOnUtc = nowProvider();
                message.Error = null;
                message.NextAttemptOnUtc = null;
                LogOutboxMessageProcessed(
                    logger,
                    message.Id,
                    message.EventType,
                    message.RetryCount + 1,
                    (nowProvider() - startedAt).TotalMilliseconds);
            }
            catch (Exception ex)
            {
                message.Error = ex.Message;
                message.RetryCount++;

                if (message.RetryCount >= Math.Max(1, options.MaxRetries))
                {
                    message.DeadLetteredOnUtc = now;
                    message.NextAttemptOnUtc = null;
                    LogOutboxMessageDeadLettered(
                        logger,
                        message.Id,
                        message.EventType,
                        message.RetryCount,
                        Math.Max(1, options.MaxRetries),
                        ex.Message);
                    continue;
                }

                message.NextAttemptOnUtc = now.Add(CalculateBackoff(message.RetryCount, options));
                LogOutboxMessageRetryScheduled(
                    logger,
                    message.Id,
                    message.EventType,
                    message.RetryCount,
                    message.NextAttemptOnUtc.Value,
                    ex.Message);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return messages.Count;
    }

    private static TimeSpan CalculateBackoff(int retryCount, OutboxProcessingOptions options)
    {
        var baseDelaySeconds = Math.Max(1, options.BaseRetryDelay.TotalSeconds);
        var maxDelaySeconds = Math.Max(baseDelaySeconds, options.MaxRetryDelay.TotalSeconds);
        var exponentialSeconds = baseDelaySeconds * Math.Pow(2, Math.Max(0, retryCount - 1));
        return TimeSpan.FromSeconds(Math.Min(exponentialSeconds, maxDelaySeconds));
    }

    private async Task DispatchToSubscribersAsync(Bud.Server.Domain.Common.Events.IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var handlerType = typeof(IDomainEventSubscriber<>).MakeGenericType(domainEvent.GetType());
        var handlers = serviceProvider.GetServices(handlerType);

        foreach (var handler in handlers)
        {
            var method = handlerType.GetMethod(nameof(IDomainEventSubscriber<Bud.Server.Domain.Common.Events.IDomainEvent>.HandleAsync), BindingFlags.Public | BindingFlags.Instance);
            if (method is null)
            {
                continue;
            }

            var task = (Task?)method.Invoke(handler, [domainEvent, cancellationToken]);
            if (task is not null)
            {
                await task;
            }
        }
    }

    [LoggerMessage(
        EventId = 3100,
        Level = LogLevel.Debug,
        Message = "Processando mensagem de outbox {OutboxMessageId}. EventType={EventType} Attempt={Attempt}")]
    private static partial void LogOutboxMessageProcessingStarted(ILogger logger, Guid outboxMessageId, string eventType, int attempt);

    [LoggerMessage(
        EventId = 3101,
        Level = LogLevel.Information,
        Message = "Mensagem de outbox {OutboxMessageId} processada com sucesso. EventType={EventType} Attempt={Attempt} ElapsedMs={ElapsedMs}")]
    private static partial void LogOutboxMessageProcessed(ILogger logger, Guid outboxMessageId, string eventType, int attempt, double elapsedMs);

    [LoggerMessage(
        EventId = 3102,
        Level = LogLevel.Warning,
        Message = "Retry agendado para mensagem de outbox {OutboxMessageId}. EventType={EventType} RetryCount={RetryCount} NextAttemptOnUtc={NextAttemptOnUtc} Error={Error}")]
    private static partial void LogOutboxMessageRetryScheduled(
        ILogger logger,
        Guid outboxMessageId,
        string eventType,
        int retryCount,
        DateTime nextAttemptOnUtc,
        string error);

    [LoggerMessage(
        EventId = 3103,
        Level = LogLevel.Error,
        Message = "Mensagem de outbox {OutboxMessageId} movida para dead-letter. EventType={EventType} RetryCount={RetryCount} MaxRetries={MaxRetries} Error={Error}")]
    private static partial void LogOutboxMessageDeadLettered(
        ILogger logger,
        Guid outboxMessageId,
        string eventType,
        int retryCount,
        int maxRetries,
        string error);
}
