using Bud.Server.Authorization.Requirements;
using Bud.Server.Authorization.ResourceScopes;
using Bud.Server.Services;
using Microsoft.AspNetCore.Authorization;

namespace Bud.Server.Authorization.Handlers;

public sealed class OrganizationOwnerHandler(IOrganizationAuthorizationService orgAuth)
    : AuthorizationHandler<OrganizationOwnerRequirement, OrganizationResource>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OrganizationOwnerRequirement requirement,
        OrganizationResource resource)
    {
        var result = await orgAuth.RequireOrgOwnerAsync(resource.OrganizationId, CancellationToken.None);
        if (result.IsSuccess)
        {
            context.Succeed(requirement);
        }
    }
}
