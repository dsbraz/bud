using Bud.Server.Application.Common.Events;
using Bud.Server.Data;
using Bud.Server.Domain.Common.Events;

namespace Bud.Server.Infrastructure.Events;

public sealed class OutboxDomainEventDispatcher(
    ApplicationDbContext dbContext,
    IOutboxEventSerializer serializer) : IDomainEventDispatcher
{
    public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var (eventType, payload) = serializer.Serialize(domainEvent);

        dbContext.OutboxMessages.Add(new OutboxMessage
        {
            OccurredOnUtc = DateTime.UtcNow,
            EventType = eventType,
            Payload = payload,
            RetryCount = 0,
            NextAttemptOnUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
