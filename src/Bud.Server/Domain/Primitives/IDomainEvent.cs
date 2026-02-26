namespace Bud.Server.Domain.Primitives;

public interface IDomainEvent
{
    DateTime OccurredOnUtc { get; }
}
