namespace Bud.Domain.Goals;

public sealed record GoalDeletedDomainEvent(
    Guid GoalId,
    Guid OrganizationId,
    string GoalName,
    Guid? ActorCollaboratorId) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
