using Bud.Server.Application.Common.Authorization;
using Bud.Server.Authorization.ResourceScopes;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Bud.Server.Authorization;

public sealed class ApplicationAuthorizationGateway(IAuthorizationService authorizationService) : IApplicationAuthorizationGateway
{
    public async Task<bool> IsOrganizationOwnerAsync(ClaimsPrincipal user, Guid organizationId, CancellationToken cancellationToken = default)
    {
        var result = await authorizationService.AuthorizeAsync(
            user,
            new OrganizationResource(organizationId),
            AuthorizationPolicies.OrganizationOwner);

        return result.Succeeded;
    }

    public async Task<bool> CanWriteOrganizationAsync(ClaimsPrincipal user, Guid organizationId, CancellationToken cancellationToken = default)
    {
        var result = await authorizationService.AuthorizeAsync(
            user,
            new OrganizationResource(organizationId),
            AuthorizationPolicies.OrganizationWrite);

        return result.Succeeded;
    }

    public async Task<bool> CanAccessTenantOrganizationAsync(ClaimsPrincipal user, Guid organizationId, CancellationToken cancellationToken = default)
    {
        var result = await authorizationService.AuthorizeAsync(
            user,
            new OrganizationResource(organizationId),
            AuthorizationPolicies.TenantOrganizationMatch);

        return result.Succeeded;
    }

    public async Task<bool> CanAccessMissionScopeAsync(
        ClaimsPrincipal user,
        Guid? workspaceId,
        Guid? teamId,
        Guid? collaboratorId,
        CancellationToken cancellationToken = default)
    {
        var result = await authorizationService.AuthorizeAsync(
            user,
            new MissionScopeResource(workspaceId, teamId, collaboratorId),
            AuthorizationPolicies.MissionScopeAccess);

        return result.Succeeded;
    }
}
