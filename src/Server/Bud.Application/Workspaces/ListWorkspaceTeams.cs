using System.Security.Claims;
using Bud.Application.Common;
using Bud.Application.Ports;
using Bud.Shared.Contracts;

namespace Bud.Application.Workspaces;

public sealed class ListWorkspaceTeams(IWorkspaceRepository workspaceRepository)
{
    public async Task<Result<PagedResult<Team>>> ExecuteAsync(
        Guid id,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);

        if (!await workspaceRepository.ExistsAsync(id, cancellationToken))
        {
            return Result<PagedResult<Team>>.NotFound(UserErrorMessages.WorkspaceNotFound);
        }

        var result = await workspaceRepository.GetTeamsAsync(id, page, pageSize, cancellationToken);
        return Result<PagedResult<Team>>.Success(result.MapPaged(x => x));
    }
}
