using System.Security.Claims;
using Bud.Shared.Contracts;
using Bud.Shared.Models;

namespace Bud.Server.Application.Workspaces;

public interface IWorkspaceCommandUseCase
{
    Task<ServiceResult<Workspace>> CreateAsync(
        ClaimsPrincipal user,
        CreateWorkspaceRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<Workspace>> UpdateAsync(
        ClaimsPrincipal user,
        Guid id,
        UpdateWorkspaceRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult> DeleteAsync(
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken = default);
}
