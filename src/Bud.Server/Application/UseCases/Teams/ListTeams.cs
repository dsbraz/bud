using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Domain.ReadModels;
using Bud.Server.Authorization;
using Bud.Server.Domain.Abstractions;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.UseCases.Teams;

public sealed class ListTeams(ITeamRepository teamRepository)
{
    public async Task<Result<Bud.Shared.Contracts.Common.PagedResult<Team>>> ExecuteAsync(
        Guid? workspaceId,
        Guid? parentTeamId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);
        var result = await teamRepository.GetAllAsync(workspaceId, parentTeamId, search, page, pageSize, cancellationToken);
        return Result<Bud.Shared.Contracts.Common.PagedResult<Team>>.Success(result.MapPaged(x => x));
    }
}

