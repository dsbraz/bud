namespace Bud.Server.Domain.Abstractions;

public interface IDomainEventConsumer<in TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    Task HandleAsync(TDomainEvent domainEvent, CancellationToken cancellationToken = default);
}
