using Bud.Api.Authorization.Requirements;
using Bud.Application.Ports;
using Microsoft.AspNetCore.Authorization;

namespace Bud.Api.Authorization.Handlers;

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
