namespace Bud.Domain.Primitives;

public interface IDomainEvent
{
    DateTime OccurredOnUtc { get; }
}
