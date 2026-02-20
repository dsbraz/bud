using Bud.Server.Authorization;
using Bud.Server.Application.Common;

namespace Bud.Server.Tests.Helpers;

public sealed class TestOrganizationAuthorizationService : IOrganizationAuthorizationService
{
    public bool ShouldAllowOwnerAccess { get; set; } = true;
    public bool ShouldAllowWriteAccess { get; set; } = true;

    public Task<Result> RequireOrgOwnerAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            ShouldAllowOwnerAccess
                ? Result.Success()
                : Result.Forbidden("Apenas o proprietário da organização pode realizar esta ação."));
    }

    public Task<Result> RequireWriteAccessAsync(Guid organizationId, Guid resourceId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            ShouldAllowWriteAccess
                ? Result.Success()
                : Result.Forbidden("Você não tem permissão de escrita nesta organização."));
    }
}
