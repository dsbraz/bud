using Bud.Application.Common;
using Bud.Application.Mapping;
using Bud.Domain.Model;
using Bud.Domain.Repositories;
using Bud.Shared.Contracts;

namespace Bud.Application.UseCases.Organizations;

public sealed class ListOrganizationWorkspaces(IOrganizationRepository organizationRepository)
{
    public async Task<Result<Bud.Shared.Contracts.Common.PagedResult<Workspace>>> ExecuteAsync(
        Guid id,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);

        if (!await organizationRepository.ExistsAsync(id, cancellationToken))
        {
            return Result<Bud.Shared.Contracts.Common.PagedResult<Workspace>>.NotFound(UserErrorMessages.OrganizationNotFound);
        }

        var result = await organizationRepository.GetWorkspacesAsync(id, page, pageSize, cancellationToken);
        return Result<Bud.Shared.Contracts.Common.PagedResult<Workspace>>.Success(result.MapPaged(x => x));
    }
}
