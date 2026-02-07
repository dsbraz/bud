using Bud.Server.Domain.Common.Events;

namespace Bud.Server.Domain.Workspaces.Events;

public sealed record WorkspaceUpdatedDomainEvent(Guid WorkspaceId, Guid OrganizationId) : IDomainEvent;
