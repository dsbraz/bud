using Bud.Shared.Contracts;
using Bud.Shared.Models;

namespace Bud.Server.Application.Abstractions;

public interface IWorkspaceCommandService
{
    Task<ServiceResult<Workspace>> CreateAsync(CreateWorkspaceRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<Workspace>> UpdateAsync(Guid id, UpdateWorkspaceRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
