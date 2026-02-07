using Bud.Server.Domain.Common.Events;

namespace Bud.Server.Domain.MissionMetrics.Events;

public sealed record MissionMetricUpdatedDomainEvent(Guid MissionMetricId, Guid MissionId, Guid OrganizationId) : IDomainEvent;
