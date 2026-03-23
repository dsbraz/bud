using Bud.Api.Authorization.Requirements;
using Bud.Application.Ports;
using Microsoft.AspNetCore.Authorization;

namespace Bud.Api.Authorization.Handlers;

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
