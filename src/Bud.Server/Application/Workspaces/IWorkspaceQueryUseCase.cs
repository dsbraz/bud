using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using Bud.Server.Application.Common;

namespace Bud.Server.Application.Workspaces;

public interface IWorkspaceQueryUseCase
{
    Task<Result<Workspace>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<PagedResult<Workspace>>> GetAllAsync(
        Guid? organizationId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Result<PagedResult<Team>>> GetTeamsAsync(
        Guid id,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
