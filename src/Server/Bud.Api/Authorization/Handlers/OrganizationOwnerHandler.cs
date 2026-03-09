using Bud.Api.Authorization.Requirements;
using Bud.Api.Authorization.ResourceScopes;
using Microsoft.AspNetCore.Authorization;

namespace Bud.Api.Authorization.Handlers;

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
