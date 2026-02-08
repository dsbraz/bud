using Bud.Server.Domain.Common.Events;

namespace Bud.Server.Domain.Workspaces.Events;

public sealed record WorkspaceDeletedDomainEvent(Guid WorkspaceId, Guid OrganizationId) : IDomainEvent;
