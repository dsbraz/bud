using Bud.Server.Application.Common;
using Bud.Server.Domain.Abstractions;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Server.Settings;
using Bud.Shared.Contracts;
using Microsoft.Extensions.Options;

namespace Bud.Server.Application.Organizations;

public sealed class ListOrganizations(IOrganizationRepository organizationRepository)
{
    public async Task<Result<Bud.Shared.Contracts.PagedResult<Organization>>> ExecuteAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);
        var result = await organizationRepository.GetAllAsync(search, page, pageSize, cancellationToken);
        return Result<Bud.Shared.Contracts.PagedResult<Organization>>.Success(result.MapPaged(x => x));
    }
}

