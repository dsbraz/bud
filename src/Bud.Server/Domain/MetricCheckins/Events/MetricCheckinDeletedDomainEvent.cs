using Bud.Server.Domain.Common.Events;

namespace Bud.Server.Domain.MetricCheckins.Events;

public sealed record MetricCheckinDeletedDomainEvent(Guid MetricCheckinId, Guid MissionMetricId, Guid OrganizationId, Guid CollaboratorId) : IDomainEvent;
