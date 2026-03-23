using Bud.Api.Authorization.Requirements;
using Bud.Api.Authorization.ResourceScopes;
using Microsoft.AspNetCore.Authorization;

namespace Bud.Api.Authorization.Handlers;

public sealed class OrganizationWriteHandler(IOrganizationAuthorizationService orgAuth)
    : AuthorizationHandler<OrganizationWriteRequirement, OrganizationResource>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OrganizationWriteRequirement requirement,
        OrganizationResource resource)
    {
        var result = await orgAuth.RequireWriteAccessAsync(resource.OrganizationId, resource.OrganizationId, CancellationToken.None);
        if (result.IsSuccess)
        {
            context.Succeed(requirement);
        }
    }
}
