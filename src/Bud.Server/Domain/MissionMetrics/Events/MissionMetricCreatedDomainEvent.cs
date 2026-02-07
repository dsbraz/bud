using Bud.Server.Domain.Common.Events;

namespace Bud.Server.Domain.MissionMetrics.Events;

public sealed record MissionMetricCreatedDomainEvent(Guid MissionMetricId, Guid MissionId, Guid OrganizationId) : IDomainEvent;
