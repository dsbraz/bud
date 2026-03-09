using Bud.Application.Common;
using Bud.Application.Mapping;
using Bud.Domain.Model;
using Bud.Domain.Repositories;
using Bud.Shared.Contracts;

namespace Bud.Application.UseCases.Organizations;

public sealed class ListOrganizations(IOrganizationRepository organizationRepository)
{
    public async Task<Result<Bud.Shared.Contracts.Common.PagedResult<Organization>>> ExecuteAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);
        var result = await organizationRepository.GetAllAsync(search, page, pageSize, cancellationToken);
        return Result<Bud.Shared.Contracts.Common.PagedResult<Organization>>.Success(result.MapPaged(x => x));
    }
}
