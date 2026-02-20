using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.Ports;
using Bud.Server.Authorization;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Application.Workspaces;

public sealed class WorkspaceCommandUseCase(
    IWorkspaceRepository workspaceRepository,
    IOrganizationRepository organizationRepository,
    IApplicationAuthorizationGateway authorizationGateway) : IWorkspaceCommandUseCase
{
    public async Task<Result<Workspace>> CreateAsync(
        ClaimsPrincipal user,
        CreateWorkspaceRequest request,
        CancellationToken cancellationToken = default)
    {
        var canCreate = await authorizationGateway.IsOrganizationOwnerAsync(user, request.OrganizationId, cancellationToken);
        if (!canCreate)
        {
            return Result<Workspace>.Forbidden("Apenas o proprietário da organização pode criar workspaces.");
        }

        if (!await organizationRepository.ExistsAsync(request.OrganizationId, cancellationToken))
        {
            return Result<Workspace>.NotFound("Organização não encontrada.");
        }

        if (!await workspaceRepository.IsNameUniqueAsync(request.OrganizationId, request.Name, ct: cancellationToken))
        {
            return Result<Workspace>.Failure("Já existe um workspace com este nome nesta organização.", ErrorType.Conflict);
        }

        try
        {
            var workspace = Workspace.Create(Guid.NewGuid(), request.OrganizationId, request.Name);

            await workspaceRepository.AddAsync(workspace, cancellationToken);
            await workspaceRepository.SaveChangesAsync(cancellationToken);

            return Result<Workspace>.Success(workspace);
        }
        catch (DomainInvariantException ex)
        {
            return Result<Workspace>.Failure(ex.Message, ErrorType.Validation);
        }
    }

    public async Task<Result<Workspace>> UpdateAsync(
        ClaimsPrincipal user,
        Guid id,
        UpdateWorkspaceRequest request,
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

        if (!await workspaceRepository.IsNameUniqueAsync(workspace.OrganizationId, request.Name, excludeId: id, ct: cancellationToken))
        {
            return Result<Workspace>.Failure("Já existe um workspace com este nome nesta organização.", ErrorType.Conflict);
        }

        try
        {
            workspace.Rename(request.Name);
            await workspaceRepository.SaveChangesAsync(cancellationToken);

            return Result<Workspace>.Success(workspace);
        }
        catch (DomainInvariantException ex)
        {
            return Result<Workspace>.Failure(ex.Message, ErrorType.Validation);
        }
    }

    public async Task<Result> DeleteAsync(
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
        await workspaceRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
