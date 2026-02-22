using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Authorization;
using Bud.Server.Domain.Abstractions;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.Workspaces;

public sealed class GetWorkspaceById(IWorkspaceRepository workspaceRepository)
{
    public async Task<Result<Workspace>> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var workspace = await workspaceRepository.GetByIdAsync(id, cancellationToken);
        return workspace is null
            ? Result<Workspace>.NotFound("Workspace não encontrado.")
            : Result<Workspace>.Success(workspace);
    }
}

