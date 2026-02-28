namespace Bud.Server.Domain.Events;

public sealed record GoalUpdatedDomainEvent(
    Guid GoalId,
    Guid OrganizationId) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
