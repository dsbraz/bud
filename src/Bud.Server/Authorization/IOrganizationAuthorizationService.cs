using Bud.Server.Application.Common;

namespace Bud.Server.Authorization;

public interface IOrganizationAuthorizationService
{
    Task<Result> RequireOrgOwnerAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<Result> RequireWriteAccessAsync(Guid organizationId, Guid resourceId, CancellationToken cancellationToken = default);
}
