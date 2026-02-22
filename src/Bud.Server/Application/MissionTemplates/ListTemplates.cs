using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Authorization;
using Bud.Server.Domain.Abstractions;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Server.MultiTenancy;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.MissionTemplates;

public sealed class ListTemplates(IMissionTemplateRepository templateRepository)
{
    public async Task<Result<Bud.Shared.Contracts.PagedResult<MissionTemplate>>> ExecuteAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);
        var result = await templateRepository.GetAllAsync(search, page, pageSize, cancellationToken);
        return Result<Bud.Shared.Contracts.PagedResult<MissionTemplate>>.Success(result.MapPaged(x => x));
    }
}
