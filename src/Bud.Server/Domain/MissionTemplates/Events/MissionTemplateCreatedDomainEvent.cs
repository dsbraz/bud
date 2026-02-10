using Bud.Server.Domain.Common.Events;

namespace Bud.Server.Domain.MissionTemplates.Events;

public sealed record MissionTemplateCreatedDomainEvent(Guid MissionTemplateId, Guid OrganizationId) : IDomainEvent;
