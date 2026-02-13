using System.Security.Claims;
using Bud.Server.Services;
using Bud.Server.Authorization;
using Bud.Server.Data;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Application.Missions;

public sealed class MissionCommandUseCase(
    IMissionService missionService,
    IMissionScopeResolver missionScopeResolver,
    IApplicationAuthorizationGateway authorizationGateway,
    IApplicationEntityLookup entityLookup,
    INotificationOrchestrator notificationOrchestrator) : IMissionCommandUseCase
{
    public async Task<ServiceResult<Mission>> CreateAsync(
        ClaimsPrincipal user,
        CreateMissionRequest request,
        CancellationToken cancellationToken = default)
    {
        var scopeResolution = await missionScopeResolver.ResolveScopeOrganizationIdAsync(
            request.ScopeType,
            request.ScopeId,
            ignoreQueryFilters: true,
            cancellationToken: cancellationToken);

        if (!scopeResolution.IsSuccess)
        {
            return ServiceResult<Mission>.NotFound(scopeResolution.Error ?? "Escopo não encontrado.");
        }

        var canCreate = await authorizationGateway.CanAccessTenantOrganizationAsync(user, scopeResolution.Value, cancellationToken);
        if (!canCreate)
        {
            return ServiceResult<Mission>.Forbidden("Você não tem permissão para criar missões nesta organização.");
        }

        var result = await missionService.CreateAsync(request, cancellationToken);
        if (result.IsSuccess)
        {
            await notificationOrchestrator.NotifyMissionCreatedAsync(
                result.Value!.Id, result.Value.OrganizationId, cancellationToken);
        }

        return result;
    }

    public async Task<ServiceResult<Mission>> UpdateAsync(
        ClaimsPrincipal user,
        Guid id,
        UpdateMissionRequest request,
        CancellationToken cancellationToken = default)
    {
        var mission = await entityLookup.GetMissionAsync(id, cancellationToken: cancellationToken);

        if (mission is null)
        {
            return ServiceResult<Mission>.NotFound("Missão não encontrada.");
        }

        var canUpdate = await authorizationGateway.CanAccessTenantOrganizationAsync(user, mission.OrganizationId, cancellationToken);
        if (!canUpdate)
        {
            return ServiceResult<Mission>.Forbidden("Você não tem permissão para atualizar missões nesta organização.");
        }

        var result = await missionService.UpdateAsync(id, request, cancellationToken);
        if (result.IsSuccess)
        {
            await notificationOrchestrator.NotifyMissionUpdatedAsync(
                result.Value!.Id, result.Value.OrganizationId, cancellationToken);
        }

        return result;
    }

    public async Task<ServiceResult> DeleteAsync(
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var mission = await entityLookup.GetMissionAsync(id, cancellationToken: cancellationToken);

        if (mission is null)
        {
            return ServiceResult.NotFound("Missão não encontrada.");
        }

        var canDelete = await authorizationGateway.CanAccessTenantOrganizationAsync(user, mission.OrganizationId, cancellationToken);
        if (!canDelete)
        {
            return ServiceResult.Forbidden("Você não tem permissão para excluir missões nesta organização.");
        }

        var result = await missionService.DeleteAsync(id, cancellationToken);
        if (result.IsSuccess)
        {
            await notificationOrchestrator.NotifyMissionDeletedAsync(
                mission.Id, mission.OrganizationId, cancellationToken);
        }

        return result;
    }
}
