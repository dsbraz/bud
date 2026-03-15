using Bud.Application.Common;
using Bud.Shared.Contracts;

namespace Bud.Application.Features.Organizations;

public sealed class ListOrganizations(IOrganizationRepository organizationRepository)
{
    public async Task<Result<PagedResult<Organization>>> ExecuteAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);
        var result = await organizationRepository.GetAllAsync(search, page, pageSize, cancellationToken);
        return Result<PagedResult<Organization>>.Success(result.MapPaged(x => x));
    }
}
