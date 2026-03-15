using System.Security.Claims;
using Bud.Application.Common;
using Bud.Application.Ports;
using Bud.Shared.Contracts;

namespace Bud.Application.Features.Workspaces;

public sealed class GetWorkspaceById(IWorkspaceRepository workspaceRepository)
{
    public async Task<Result<Workspace>> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var workspace = await workspaceRepository.GetByIdAsync(id, cancellationToken);
        return workspace is null
            ? Result<Workspace>.NotFound(UserErrorMessages.WorkspaceNotFound)
            : Result<Workspace>.Success(workspace);
    }
}

