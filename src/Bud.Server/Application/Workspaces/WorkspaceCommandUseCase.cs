using System.Security.Claims;
using Bud.Server.Services;
using Bud.Server.Authorization;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Application.Workspaces;

public sealed class WorkspaceCommandUseCase(
    IWorkspaceService workspaceService,
    IApplicationAuthorizationGateway authorizationGateway,
    IEntityLookupService entityLookup) : IWorkspaceCommandUseCase
{
    public async Task<ServiceResult<Workspace>> CreateAsync(
        ClaimsPrincipal user,
        CreateWorkspaceRequest request,
        CancellationToken cancellationToken = default)
    {
        var canCreate = await authorizationGateway.IsOrganizationOwnerAsync(user, request.OrganizationId, cancellationToken);
        if (!canCreate)
        {
            return ServiceResult<Workspace>.Forbidden("Apenas o proprietário da organização pode criar workspaces.");
        }

        return await workspaceService.CreateAsync(request, cancellationToken);
    }

    public async Task<ServiceResult<Workspace>> UpdateAsync(
        ClaimsPrincipal user,
        Guid id,
        UpdateWorkspaceRequest request,
        CancellationToken cancellationToken = default)
    {
        var workspace = await entityLookup.GetWorkspaceAsync(id, cancellationToken);

        if (workspace is null)
        {
            return ServiceResult<Workspace>.NotFound("Workspace não encontrado.");
        }

        var canUpdate = await authorizationGateway.CanWriteOrganizationAsync(user, workspace.OrganizationId, cancellationToken);
        if (!canUpdate)
        {
            return ServiceResult<Workspace>.Forbidden("Você não tem permissão para atualizar este workspace.");
        }

        return await workspaceService.UpdateAsync(id, request, cancellationToken);
    }

    public async Task<ServiceResult> DeleteAsync(
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var workspace = await entityLookup.GetWorkspaceAsync(id, cancellationToken);

        if (workspace is null)
        {
            return ServiceResult.NotFound("Workspace não encontrado.");
        }

        var canDelete = await authorizationGateway.CanWriteOrganizationAsync(user, workspace.OrganizationId, cancellationToken);
        if (!canDelete)
        {
            return ServiceResult.Forbidden("Você não tem permissão para excluir este workspace.");
        }

        return await workspaceService.DeleteAsync(id, cancellationToken);
    }
}
