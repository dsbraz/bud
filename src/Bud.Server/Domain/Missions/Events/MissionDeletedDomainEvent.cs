using Bud.Server.Domain.Common.Events;

namespace Bud.Server.Domain.Missions.Events;

public sealed record MissionDeletedDomainEvent(Guid MissionId, Guid OrganizationId) : IDomainEvent;
