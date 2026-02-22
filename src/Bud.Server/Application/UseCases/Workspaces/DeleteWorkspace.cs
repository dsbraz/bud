using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Authorization;
using Bud.Server.Domain.Abstractions;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.UseCases.Workspaces;

public sealed class DeleteWorkspace(
    IWorkspaceRepository workspaceRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result> ExecuteAsync(
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var workspace = await workspaceRepository.GetByIdAsync(id, cancellationToken);
        if (workspace is null)
        {
            return Result.NotFound("Workspace não encontrado.");
        }

        var canDelete = await authorizationGateway.CanWriteOrganizationAsync(user, workspace.OrganizationId, cancellationToken);
        if (!canDelete)
        {
            return Result.Forbidden("Você não tem permissão para excluir este workspace.");
        }

        if (await workspaceRepository.HasMissionsAsync(id, cancellationToken))
        {
            return Result.Failure(
                "Não é possível excluir o workspace porque existem missões associadas a ele.",
                ErrorType.Conflict);
        }

        await workspaceRepository.RemoveAsync(workspace, cancellationToken);
        await unitOfWork.CommitAsync(workspaceRepository.SaveChangesAsync, cancellationToken);

        return Result.Success();
    }
}

