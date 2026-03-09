using System.Security.Claims;
using Bud.Application.Common;
using Bud.Application.Mapping;
using Bud.Application.Ports;
using Bud.Domain.Model;
using Bud.Domain.Repositories;
using Bud.Shared.Contracts;

namespace Bud.Application.UseCases.Workspaces;

public sealed class ListWorkspaceTeams(IWorkspaceRepository workspaceRepository)
{
    public async Task<Result<Bud.Shared.Contracts.Common.PagedResult<Team>>> ExecuteAsync(
        Guid id,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);

        if (!await workspaceRepository.ExistsAsync(id, cancellationToken))
        {
            return Result<Bud.Shared.Contracts.Common.PagedResult<Team>>.NotFound(UserErrorMessages.WorkspaceNotFound);
        }

        var result = await workspaceRepository.GetTeamsAsync(id, page, pageSize, cancellationToken);
        return Result<Bud.Shared.Contracts.Common.PagedResult<Team>>.Success(result.MapPaged(x => x));
    }
}
