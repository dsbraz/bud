using Bud.Server.Domain.Common.Events;

namespace Bud.Server.Domain.Teams.Events;

public sealed record TeamUpdatedDomainEvent(Guid TeamId, Guid OrganizationId, Guid WorkspaceId) : IDomainEvent;
