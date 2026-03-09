using Bud.Api.Authorization.Requirements;
using Bud.Api.Authorization.ResourceScopes;
using Bud.Application.Ports;
using Microsoft.AspNetCore.Authorization;

namespace Bud.Api.Authorization.Handlers;

public sealed class TenantOrganizationMatchHandler(ITenantProvider tenantProvider)
    : AuthorizationHandler<TenantOrganizationMatchRequirement, OrganizationResource>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TenantOrganizationMatchRequirement requirement,
        OrganizationResource resource)
    {
        if (tenantProvider.IsGlobalAdmin)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (tenantProvider.TenantId.HasValue && tenantProvider.TenantId.Value == resource.OrganizationId)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
