using Bud.Server.Domain.Common.Events;

namespace Bud.Server.Domain.MissionTemplates.Events;

public sealed record MissionTemplateUpdatedDomainEvent(Guid MissionTemplateId, Guid OrganizationId) : IDomainEvent;
