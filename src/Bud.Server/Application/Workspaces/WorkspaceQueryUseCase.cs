using Bud.Shared.Contracts;
using Bud.Shared.Models;

namespace Bud.Server.Application.Workspaces;

public sealed class WorkspaceQueryUseCase(IWorkspaceService workspaceService) : IWorkspaceQueryUseCase
{
    public Task<ServiceResult<Workspace>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => workspaceService.GetByIdAsync(id, cancellationToken);

    public Task<ServiceResult<PagedResult<Workspace>>> GetAllAsync(
        Guid? organizationId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
        => workspaceService.GetAllAsync(organizationId, search, page, pageSize, cancellationToken);

    public Task<ServiceResult<PagedResult<Team>>> GetTeamsAsync(
        Guid id,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
        => workspaceService.GetTeamsAsync(id, page, pageSize, cancellationToken);
}
