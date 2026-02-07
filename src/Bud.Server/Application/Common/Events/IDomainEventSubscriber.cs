using Bud.Server.Domain.Common.Events;

namespace Bud.Server.Application.Common.Events;

public interface IDomainEventSubscriber<in TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    Task HandleAsync(TDomainEvent domainEvent, CancellationToken cancellationToken = default);
}
