using Bud.Server.Services;

namespace Bud.Server.Tests.Helpers;

public sealed class TestOrganizationAuthorizationService : IOrganizationAuthorizationService
{
    public bool ShouldAllowOwnerAccess { get; set; } = true;
    public bool ShouldAllowWriteAccess { get; set; } = true;

    public Task<ServiceResult> RequireOrgOwnerAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            ShouldAllowOwnerAccess
                ? ServiceResult.Success()
                : ServiceResult.Forbidden("Apenas o proprietário da organização pode realizar esta ação."));
    }

    public Task<ServiceResult> RequireWriteAccessAsync(Guid organizationId, Guid resourceId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            ShouldAllowWriteAccess
                ? ServiceResult.Success()
                : ServiceResult.Forbidden("Você não tem permissão de escrita nesta organização."));
    }
}
