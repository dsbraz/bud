namespace Bud.Domain.Goals.Events;

public sealed record GoalDeletedDomainEvent(
    Guid GoalId,
    Guid OrganizationId,
    string GoalName,
    Guid? ActorCollaboratorId) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
