using Bud.Server.Domain.Abstractions;

namespace Bud.Server.Domain.Events;

public sealed record MissionUpdatedDomainEvent(
    Guid MissionId,
    Guid OrganizationId) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
