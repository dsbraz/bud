using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Domain.ReadModels;
using Bud.Server.Authorization;
using Bud.Server.Domain.Abstractions;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.Teams;

public sealed class ListSubTeams(ITeamRepository teamRepository)
{
    public async Task<Result<Bud.Shared.Contracts.PagedResult<Team>>> ExecuteAsync(
        Guid id,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);

        if (!await teamRepository.ExistsAsync(id, cancellationToken))
        {
            return Result<Bud.Shared.Contracts.PagedResult<Team>>.NotFound("Time não encontrado.");
        }

        var result = await teamRepository.GetSubTeamsAsync(id, page, pageSize, cancellationToken);
        return Result<Bud.Shared.Contracts.PagedResult<Team>>.Success(result.MapPaged(x => x));
    }
}

