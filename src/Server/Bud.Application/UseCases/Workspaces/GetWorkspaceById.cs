using System.Security.Claims;
using Bud.Application.Common;
using Bud.Application.Mapping;
using Bud.Application.Ports;
using Bud.Domain.Model;
using Bud.Domain.Repositories;
using Bud.Shared.Contracts;

namespace Bud.Application.UseCases.Workspaces;

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

