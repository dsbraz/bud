using System.Security.Claims;
using Bud.Server.Services;
using Bud.Server.Authorization;
using Bud.Server.Data;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Application.Teams;

public sealed class TeamCommandUseCase(
    ITeamService teamService,
    IApplicationAuthorizationGateway authorizationGateway,
    IApplicationEntityLookup entityLookup) : ITeamCommandUseCase
{
    public async Task<ServiceResult<Team>> CreateAsync(
        ClaimsPrincipal user,
        CreateTeamRequest request,
        CancellationToken cancellationToken = default)
    {
        var workspace = await entityLookup.GetWorkspaceAsync(request.WorkspaceId, cancellationToken);

        if (workspace is null)
        {
            return ServiceResult<Team>.NotFound("Workspace não encontrado.");
        }

        var canCreate = await authorizationGateway.IsOrganizationOwnerAsync(user, workspace.OrganizationId, cancellationToken);
        if (!canCreate)
        {
            return ServiceResult<Team>.Forbidden("Apenas o proprietário da organização pode criar times.");
        }

        return await teamService.CreateAsync(request, cancellationToken);
    }

    public async Task<ServiceResult<Team>> UpdateAsync(
        ClaimsPrincipal user,
        Guid id,
        UpdateTeamRequest request,
        CancellationToken cancellationToken = default)
    {
        var team = await entityLookup.GetTeamAsync(id, cancellationToken);

        if (team is null)
        {
            return ServiceResult<Team>.NotFound("Time não encontrado.");
        }

        var canUpdate = await authorizationGateway.CanWriteOrganizationAsync(user, team.OrganizationId, cancellationToken);
        if (!canUpdate)
        {
            return ServiceResult<Team>.Forbidden("Você não tem permissão para atualizar este time.");
        }

        return await teamService.UpdateAsync(id, request, cancellationToken);
    }

    public async Task<ServiceResult> DeleteAsync(
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var team = await entityLookup.GetTeamAsync(id, cancellationToken);

        if (team is null)
        {
            return ServiceResult.NotFound("Time não encontrado.");
        }

        var canDelete = await authorizationGateway.CanWriteOrganizationAsync(user, team.OrganizationId, cancellationToken);
        if (!canDelete)
        {
            return ServiceResult.Forbidden("Você não tem permissão para excluir este time.");
        }

        return await teamService.DeleteAsync(id, cancellationToken);
    }

    public async Task<ServiceResult> UpdateCollaboratorsAsync(
        ClaimsPrincipal user,
        Guid id,
        UpdateTeamCollaboratorsRequest request,
        CancellationToken cancellationToken = default)
    {
        var team = await entityLookup.GetTeamAsync(id, cancellationToken);

        if (team is null)
        {
            return ServiceResult.NotFound("Time não encontrado.");
        }

        var canManage = await authorizationGateway.IsOrganizationOwnerAsync(user, team.OrganizationId, cancellationToken);
        if (!canManage)
        {
            return ServiceResult.Forbidden("Apenas o proprietário da organização pode atribuir colaboradores.");
        }

        return await teamService.UpdateCollaboratorsAsync(id, request, cancellationToken);
    }
}
