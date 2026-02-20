using Bud.Server.Application.Common;
using Bud.Server.Application.Ports;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Application.Workspaces;

public sealed class WorkspaceQueryUseCase(IWorkspaceRepository workspaceRepository) : IWorkspaceQueryUseCase
{
    public async Task<Result<Workspace>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var workspace = await workspaceRepository.GetByIdAsync(id, cancellationToken);
        return workspace is null
            ? Result<Workspace>.NotFound("Workspace não encontrado.")
            : Result<Workspace>.Success(workspace);
    }

    public async Task<Result<PagedResult<Workspace>>> GetAllAsync(
        Guid? organizationId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);
        var result = await workspaceRepository.GetAllAsync(organizationId, search, page, pageSize, cancellationToken);
        return Result<PagedResult<Workspace>>.Success(result);
    }

    public async Task<Result<PagedResult<Team>>> GetTeamsAsync(
        Guid id,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);

        if (!await workspaceRepository.ExistsAsync(id, cancellationToken))
        {
            return Result<PagedResult<Team>>.NotFound("Workspace não encontrado.");
        }

        var result = await workspaceRepository.GetTeamsAsync(id, page, pageSize, cancellationToken);
        return Result<PagedResult<Team>>.Success(result);
    }
}
