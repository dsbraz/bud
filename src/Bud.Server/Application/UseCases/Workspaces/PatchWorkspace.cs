using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Authorization;
using Bud.Server.Domain.Abstractions;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.UseCases.Workspaces;

public sealed class PatchWorkspace(
    IWorkspaceRepository workspaceRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<Workspace>> ExecuteAsync(
        ClaimsPrincipal user,
        Guid id,
        PatchWorkspaceRequest request,
        CancellationToken cancellationToken = default)
    {
        var workspace = await workspaceRepository.GetByIdAsync(id, cancellationToken);
        if (workspace is null)
        {
            return Result<Workspace>.NotFound("Workspace não encontrado.");
        }

        var canUpdate = await authorizationGateway.CanWriteOrganizationAsync(user, workspace.OrganizationId, cancellationToken);
        if (!canUpdate)
        {
            return Result<Workspace>.Forbidden("Você não tem permissão para atualizar este workspace.");
        }

        try
        {
            if (request.Name.HasValue)
            {
                var newName = request.Name.Value ?? string.Empty;

                if (!await workspaceRepository.IsNameUniqueAsync(workspace.OrganizationId, newName, excludeId: id, ct: cancellationToken))
                {
                    return Result<Workspace>.Failure("Já existe um workspace com este nome nesta organização.", ErrorType.Conflict);
                }

                workspace.Rename(newName);
            }

            await unitOfWork.CommitAsync(workspaceRepository.SaveChangesAsync, cancellationToken);

            return Result<Workspace>.Success(workspace);
        }
        catch (DomainInvariantException ex)
        {
            return Result<Workspace>.Failure(ex.Message, ErrorType.Validation);
        }
    }
}
