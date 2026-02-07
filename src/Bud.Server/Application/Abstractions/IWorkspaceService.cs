using Bud.Shared.Contracts;
using Bud.Shared.Models;

namespace Bud.Server.Application.Abstractions;

public interface IWorkspaceService
{
    Task<ServiceResult<Workspace>> CreateAsync(CreateWorkspaceRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<Workspace>> UpdateAsync(Guid id, UpdateWorkspaceRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ServiceResult<Workspace>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ServiceResult<PagedResult<Workspace>>> GetAllAsync(Guid? organizationId, string? search, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ServiceResult<PagedResult<Team>>> GetTeamsAsync(Guid id, int page, int pageSize, CancellationToken cancellationToken = default);
}
