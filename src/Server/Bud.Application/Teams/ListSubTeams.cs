using System.Security.Claims;
using Bud.Application.Common;
using Bud.Application.Ports;
using Bud.Shared.Contracts;

namespace Bud.Application.Teams;

public sealed class ListSubTeams(ITeamRepository teamRepository)
{
    public async Task<Result<PagedResult<Team>>> ExecuteAsync(
        Guid id,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);

        if (!await teamRepository.ExistsAsync(id, cancellationToken))
        {
            return Result<PagedResult<Team>>.NotFound(UserErrorMessages.TeamNotFound);
        }

        var result = await teamRepository.GetSubTeamsAsync(id, page, pageSize, cancellationToken);
        return Result<PagedResult<Team>>.Success(result.MapPaged(x => x));
    }
}

