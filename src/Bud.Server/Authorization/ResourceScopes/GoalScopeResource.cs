namespace Bud.Server.Authorization.ResourceScopes;

public sealed record GoalScopeResource(Guid? WorkspaceId, Guid? TeamId, Guid? CollaboratorId);
