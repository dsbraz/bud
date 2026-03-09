using Bud.Api.Authorization;
using Bud.Application.Common;
using Bud.Application.Mapping;
using Bud.Application.Ports;

namespace Bud.Api.UnitTests.Helpers;

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
