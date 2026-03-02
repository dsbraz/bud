namespace Bud.Server.Domain.Events;

public sealed record GoalCreatedDomainEvent(
    Guid GoalId,
    Guid OrganizationId) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
