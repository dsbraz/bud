using System.Security.Claims;
using Bud.Server.Authorization;
using Bud.Server.Data;
using Bud.Server.Services;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Application.MissionObjectives;

public sealed class MissionObjectiveCommandUseCase(
    IMissionObjectiveService objectiveService,
    IApplicationAuthorizationGateway authorizationGateway,
    IApplicationEntityLookup entityLookup) : IMissionObjectiveCommandUseCase
{
    public async Task<ServiceResult<MissionObjective>> CreateAsync(
        ClaimsPrincipal user,
        CreateMissionObjectiveRequest request,
        CancellationToken cancellationToken = default)
    {
        var mission = await entityLookup.GetMissionAsync(request.MissionId, ignoreQueryFilters: true, cancellationToken: cancellationToken);

        if (mission is null)
        {
            return ServiceResult<MissionObjective>.NotFound("Missão não encontrada.");
        }

        var canCreate = await authorizationGateway.CanAccessTenantOrganizationAsync(user, mission.OrganizationId, cancellationToken);
        if (!canCreate)
        {
            return ServiceResult<MissionObjective>.Forbidden("Você não tem permissão para criar objetivos nesta missão.");
        }

        return await objectiveService.CreateAsync(request, cancellationToken);
    }

    public async Task<ServiceResult<MissionObjective>> UpdateAsync(
        ClaimsPrincipal user,
        Guid id,
        UpdateMissionObjectiveRequest request,
        CancellationToken cancellationToken = default)
    {
        var objective = await entityLookup.GetMissionObjectiveAsync(id, ignoreQueryFilters: true, cancellationToken: cancellationToken);

        if (objective is null)
        {
            return ServiceResult<MissionObjective>.NotFound("Objetivo não encontrado.");
        }

        var canUpdate = await authorizationGateway.CanAccessTenantOrganizationAsync(user, objective.OrganizationId, cancellationToken);
        if (!canUpdate)
        {
            return ServiceResult<MissionObjective>.Forbidden("Você não tem permissão para atualizar objetivos nesta missão.");
        }

        return await objectiveService.UpdateAsync(id, request, cancellationToken);
    }

    public async Task<ServiceResult> DeleteAsync(
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var objective = await entityLookup.GetMissionObjectiveAsync(id, ignoreQueryFilters: true, cancellationToken: cancellationToken);

        if (objective is null)
        {
            return ServiceResult.NotFound("Objetivo não encontrado.");
        }

        var canDelete = await authorizationGateway.CanAccessTenantOrganizationAsync(user, objective.OrganizationId, cancellationToken);
        if (!canDelete)
        {
            return ServiceResult.Forbidden("Você não tem permissão para excluir objetivos nesta missão.");
        }

        return await objectiveService.DeleteAsync(id, cancellationToken);
    }
}
