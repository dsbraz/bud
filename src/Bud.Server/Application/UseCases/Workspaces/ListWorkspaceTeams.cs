using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Authorization;
using Bud.Server.Domain.Abstractions;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.UseCases.Workspaces;

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
            return Result<Bud.Shared.Contracts.Common.PagedResult<Team>>.NotFound("Workspace não encontrado.");
        }

        var result = await workspaceRepository.GetTeamsAsync(id, page, pageSize, cancellationToken);
        return Result<Bud.Shared.Contracts.Common.PagedResult<Team>>.Success(result.MapPaged(x => x));
    }
}
