using Bud.Server.Domain.Common.Events;

namespace Bud.Server.Domain.Teams.Events;

public sealed record TeamCreatedDomainEvent(Guid TeamId, Guid OrganizationId, Guid WorkspaceId) : IDomainEvent;
