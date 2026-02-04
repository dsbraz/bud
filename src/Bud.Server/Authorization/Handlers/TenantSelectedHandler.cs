using Bud.Server.Authorization.Requirements;
using Bud.Server.MultiTenancy;
using Microsoft.AspNetCore.Authorization;

namespace Bud.Server.Authorization.Handlers;

public sealed class TenantSelectedHandler(ITenantProvider tenantProvider)
    : AuthorizationHandler<TenantSelectedRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TenantSelectedRequirement requirement)
    {
        if (tenantProvider.IsGlobalAdmin || tenantProvider.TenantId.HasValue)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
