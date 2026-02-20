using System.Security.Claims;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using Bud.Server.Application.Common;

namespace Bud.Server.Application.Workspaces;

public interface IWorkspaceCommandUseCase
{
    Task<Result<Workspace>> CreateAsync(
        ClaimsPrincipal user,
        CreateWorkspaceRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<Workspace>> UpdateAsync(
        ClaimsPrincipal user,
        Guid id,
        UpdateWorkspaceRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken = default);
}
