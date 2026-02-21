using Bud.Server.Domain.Abstractions;

namespace Bud.Server.Domain.Events;

public sealed record MetricCheckinCreatedDomainEvent(
    Guid CheckinId,
    Guid MissionMetricId,
    Guid OrganizationId,
    Guid? ExcludeCollaboratorId) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
