namespace Bud.Server.Domain.Common.Events;

public interface IVersionedDomainEvent : IDomainEvent
{
    int Version { get; }
}
