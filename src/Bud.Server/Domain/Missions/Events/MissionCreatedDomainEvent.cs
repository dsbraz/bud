using Bud.Server.Domain.Common.Events;

namespace Bud.Server.Domain.Missions.Events;

public sealed record MissionCreatedDomainEvent(Guid MissionId, Guid OrganizationId) : IDomainEvent;
