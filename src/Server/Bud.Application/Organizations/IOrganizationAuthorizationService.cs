using Bud.Application.Common;

namespace Bud.Application.Organizations;

public interface IOrganizationAuthorizationService
{
    Task<Result> RequireOrgOwnerAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<Result> RequireWriteAccessAsync(Guid organizationId, Guid resourceId, CancellationToken cancellationToken = default);
}
