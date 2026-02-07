using Bud.Shared.Contracts;
using Bud.Shared.Models;

namespace Bud.Server.Application.Workspaces;

public interface IWorkspaceQueryUseCase
{
    Task<ServiceResult<Workspace>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ServiceResult<PagedResult<Workspace>>> GetAllAsync(
        Guid? organizationId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<PagedResult<Team>>> GetTeamsAsync(
        Guid id,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
