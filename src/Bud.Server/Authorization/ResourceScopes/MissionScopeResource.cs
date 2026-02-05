namespace Bud.Server.Authorization.ResourceScopes;

public sealed record MissionScopeResource(Guid? WorkspaceId, Guid? TeamId, Guid? CollaboratorId);
