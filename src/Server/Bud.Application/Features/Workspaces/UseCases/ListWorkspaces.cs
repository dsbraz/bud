using System.Security.Claims;
using Bud.Application.Common;
using Bud.Application.Ports;
using Bud.Shared.Contracts;

namespace Bud.Application.Features.Workspaces;

public sealed class ListWorkspaces(IWorkspaceRepository workspaceRepository)
{
    public async Task<Result<PagedResult<Workspace>>> ExecuteAsync(
        Guid? organizationId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);
        var result = await workspaceRepository.GetAllAsync(organizationId, search, page, pageSize, cancellationToken);
        return Result<PagedResult<Workspace>>.Success(result.MapPaged(x => x));
    }
}

