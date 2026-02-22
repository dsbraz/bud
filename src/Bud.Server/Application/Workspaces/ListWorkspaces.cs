using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Authorization;
using Bud.Server.Domain.Abstractions;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.Workspaces;

public sealed class ListWorkspaces(IWorkspaceRepository workspaceRepository)
{
    public async Task<Result<Bud.Shared.Contracts.PagedResult<Workspace>>> ExecuteAsync(
        Guid? organizationId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);
        var result = await workspaceRepository.GetAllAsync(organizationId, search, page, pageSize, cancellationToken);
        return Result<Bud.Shared.Contracts.PagedResult<Workspace>>.Success(result.MapPaged(x => x));
    }
}

