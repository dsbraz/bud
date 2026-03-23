using System.Security.Claims;

namespace Bud.Application.Ports;

public interface IApplicationAuthorizationGateway
{
    Task<bool> IsOrganizationOwnerAsync(ClaimsPrincipal user, Guid organizationId, CancellationToken cancellationToken = default);

    Task<bool> CanWriteOrganizationAsync(ClaimsPrincipal user, Guid organizationId, CancellationToken cancellationToken = default);
}
