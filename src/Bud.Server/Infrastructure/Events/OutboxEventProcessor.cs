using Bud.Server.Application.Common.Events;
using Bud.Server.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace Bud.Server.Infrastructure.Events;

public sealed class OutboxEventProcessor(
    ApplicationDbContext dbContext,
    IServiceProvider serviceProvider,
    IOutboxEventSerializer serializer,
    IOptions<OutboxProcessingOptions> processingOptions,
    Func<DateTime>? utcNowProvider = null)
{
    public async Task<int> ProcessPendingAsync(int batchSize = 100, CancellationToken cancellationToken = default)
    {
        var now = (utcNowProvider ?? (() => DateTime.UtcNow))();
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
            try
            {
                var domainEvent = serializer.Deserialize(message.EventType, message.Payload);
                await DispatchToSubscribersAsync(domainEvent, cancellationToken);
                message.ProcessedOnUtc = DateTime.UtcNow;
                message.Error = null;
                message.NextAttemptOnUtc = null;
            }
            catch (Exception ex)
            {
                message.Error = ex.Message;
                message.RetryCount++;

                if (message.RetryCount >= Math.Max(1, options.MaxRetries))
                {
                    message.DeadLetteredOnUtc = now;
                    message.NextAttemptOnUtc = null;
                    continue;
                }

                message.NextAttemptOnUtc = now.Add(CalculateBackoff(message.RetryCount, options));
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
}
