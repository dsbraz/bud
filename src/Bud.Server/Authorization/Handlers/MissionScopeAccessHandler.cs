using Bud.Server.Authorization.Requirements;
using Bud.Server.Authorization.ResourceScopes;
using Bud.Server.Data;
using Bud.Server.MultiTenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Bud.Server.Authorization.Handlers;

public sealed class MissionScopeAccessHandler(
    ITenantProvider tenantProvider,
    ApplicationDbContext dbContext)
    : AuthorizationHandler<MissionScopeAccessRequirement, MissionScopeResource>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        MissionScopeAccessRequirement requirement,
        MissionScopeResource resource)
    {
        if (tenantProvider.IsGlobalAdmin)
        {
            context.Succeed(requirement);
            return;
        }

        var collaboratorId = tenantProvider.CollaboratorId;
        if (!collaboratorId.HasValue)
        {
            return;
        }

        var isOrgScoped = resource.WorkspaceId is null
                          && resource.TeamId is null
                          && resource.CollaboratorId is null;

        if (isOrgScoped)
        {
            context.Succeed(requirement);
            return;
        }

        if (resource.CollaboratorId.HasValue)
        {
            if (collaboratorId.Value == resource.CollaboratorId.Value)
            {
                context.Succeed(requirement);
            }

            return;
        }

        if (resource.TeamId.HasValue)
        {
            var belongsToTeam = await dbContext.Set<Bud.Shared.Domain.CollaboratorTeam>()
                .AnyAsync(ct => ct.CollaboratorId == collaboratorId.Value
                                && ct.TeamId == resource.TeamId.Value);

            if (belongsToTeam)
            {
                context.Succeed(requirement);
            }

            return;
        }

        if (resource.WorkspaceId.HasValue)
        {
            var belongsToWorkspace = await dbContext.Set<Bud.Shared.Domain.CollaboratorTeam>()
                .AnyAsync(ct => ct.CollaboratorId == collaboratorId.Value
                                && ct.Team.WorkspaceId == resource.WorkspaceId.Value);

            if (belongsToWorkspace)
            {
                context.Succeed(requirement);
            }
        }
    }
}
