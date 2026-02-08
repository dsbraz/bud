using Bud.Server.Domain.Common.Events;

namespace Bud.Server.Domain.Teams.Events;

public sealed record TeamDeletedDomainEvent(Guid TeamId, Guid OrganizationId, Guid WorkspaceId) : IDomainEvent;
