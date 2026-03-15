using Bud.Application.Common;
using Bud.Shared.Contracts;

namespace Bud.Application.Features.Organizations;

public sealed class ListOrganizationWorkspaces(IOrganizationRepository organizationRepository)
{
    public async Task<Result<PagedResult<Workspace>>> ExecuteAsync(
        Guid id,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);

        if (!await organizationRepository.ExistsAsync(id, cancellationToken))
        {
            return Result<PagedResult<Workspace>>.NotFound(UserErrorMessages.OrganizationNotFound);
        }

        var result = await organizationRepository.GetWorkspacesAsync(id, page, pageSize, cancellationToken);
        return Result<PagedResult<Workspace>>.Success(result.MapPaged(x => x));
    }
}
