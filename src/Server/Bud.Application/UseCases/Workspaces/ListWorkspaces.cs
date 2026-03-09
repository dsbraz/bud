using System.Security.Claims;
using Bud.Application.Common;
using Bud.Application.Mapping;
using Bud.Application.Ports;
using Bud.Domain.Model;
using Bud.Domain.Repositories;
using Bud.Shared.Contracts;

namespace Bud.Application.UseCases.Workspaces;

public sealed class ListWorkspaces(IWorkspaceRepository workspaceRepository)
{
    public async Task<Result<Bud.Shared.Contracts.Common.PagedResult<Workspace>>> ExecuteAsync(
        Guid? organizationId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);
        var result = await workspaceRepository.GetAllAsync(organizationId, search, page, pageSize, cancellationToken);
        return Result<Bud.Shared.Contracts.Common.PagedResult<Workspace>>.Success(result.MapPaged(x => x));
    }
}

