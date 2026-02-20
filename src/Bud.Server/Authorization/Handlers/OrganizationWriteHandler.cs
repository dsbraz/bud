using Bud.Server.Authorization.Requirements;
using Bud.Server.Authorization.ResourceScopes;
using Microsoft.AspNetCore.Authorization;

namespace Bud.Server.Authorization.Handlers;

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
