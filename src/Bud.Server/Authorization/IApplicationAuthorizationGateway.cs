using System.Security.Claims;

namespace Bud.Server.Authorization;

public interface IApplicationAuthorizationGateway
{
    Task<bool> IsOrganizationOwnerAsync(ClaimsPrincipal user, Guid organizationId, CancellationToken cancellationToken = default);

    Task<bool> CanWriteOrganizationAsync(ClaimsPrincipal user, Guid organizationId, CancellationToken cancellationToken = default);

    Task<bool> CanAccessTenantOrganizationAsync(ClaimsPrincipal user, Guid organizationId, CancellationToken cancellationToken = default);

    Task<bool> CanAccessMissionScopeAsync(
        ClaimsPrincipal user,
        Guid? workspaceId,
        Guid? teamId,
        Guid? collaboratorId,
        CancellationToken cancellationToken = default);
}
