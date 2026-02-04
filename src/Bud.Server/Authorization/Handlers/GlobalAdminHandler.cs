using Bud.Server.Authorization.Requirements;
using Bud.Server.MultiTenancy;
using Microsoft.AspNetCore.Authorization;

namespace Bud.Server.Authorization.Handlers;

public sealed class GlobalAdminHandler(ITenantProvider tenantProvider)
    : AuthorizationHandler<GlobalAdminRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        GlobalAdminRequirement requirement)
    {
        if (tenantProvider.IsGlobalAdmin)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
