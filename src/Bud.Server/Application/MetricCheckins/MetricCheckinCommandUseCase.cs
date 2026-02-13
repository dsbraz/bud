using System.Security.Claims;
using Bud.Server.Services;
using Bud.Server.Authorization;
using Bud.Server.Data;
using Bud.Server.MultiTenancy;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Application.MetricCheckins;

public sealed class MetricCheckinCommandUseCase(
    IMetricCheckinService checkinService,
    IApplicationAuthorizationGateway authorizationGateway,
    ITenantProvider tenantProvider,
    IApplicationEntityLookup entityLookup,
    INotificationOrchestrator notificationOrchestrator) : IMetricCheckinCommandUseCase
{
    public async Task<ServiceResult<MetricCheckin>> CreateAsync(
        ClaimsPrincipal user,
        CreateMetricCheckinRequest request,
        CancellationToken cancellationToken = default)
    {
        var metric = await entityLookup.GetMissionMetricAsync(
            request.MissionMetricId,
            includeMission: true,
            cancellationToken: cancellationToken);

        if (metric is null)
        {
            return ServiceResult<MetricCheckin>.NotFound("Métrica não encontrada.");
        }

        var hasTenantAccess = await authorizationGateway.CanAccessTenantOrganizationAsync(user, metric.OrganizationId, cancellationToken);
        if (!hasTenantAccess)
        {
            return ServiceResult<MetricCheckin>.Forbidden("Você não tem permissão para criar check-ins nesta métrica.");
        }

        var mission = metric.Mission;
        var hasScopeAccess = await authorizationGateway.CanAccessMissionScopeAsync(
            user,
            mission.WorkspaceId,
            mission.TeamId,
            mission.CollaboratorId,
            cancellationToken);
        if (!hasScopeAccess)
        {
            return ServiceResult<MetricCheckin>.Forbidden("Você não tem permissão para fazer check-in nesta métrica.");
        }

        var collaboratorId = tenantProvider.CollaboratorId;
        if (!collaboratorId.HasValue)
        {
            return ServiceResult<MetricCheckin>.Forbidden("Colaborador não identificado.");
        }

        var collaborator = await entityLookup.GetCollaboratorAsync(collaboratorId.Value, cancellationToken);
        if (collaborator is null)
        {
            return ServiceResult<MetricCheckin>.Forbidden("Colaborador não encontrado.");
        }

        var result = await checkinService.CreateAsync(request, collaboratorId.Value, cancellationToken);
        if (result.IsSuccess)
        {
            await notificationOrchestrator.NotifyMetricCheckinCreatedAsync(
                result.Value!.Id,
                result.Value.MissionMetricId,
                result.Value.OrganizationId,
                result.Value.CollaboratorId,
                cancellationToken);
        }

        return result;
    }

    public async Task<ServiceResult<MetricCheckin>> UpdateAsync(
        ClaimsPrincipal user,
        Guid id,
        UpdateMetricCheckinRequest request,
        CancellationToken cancellationToken = default)
    {
        var checkin = await entityLookup.GetMetricCheckinAsync(id, ignoreQueryFilters: true, cancellationToken: cancellationToken);

        if (checkin is null)
        {
            return ServiceResult<MetricCheckin>.NotFound("Check-in não encontrado.");
        }

        var hasTenantAccess = await authorizationGateway.CanAccessTenantOrganizationAsync(user, checkin.OrganizationId, cancellationToken);
        if (!hasTenantAccess)
        {
            return ServiceResult<MetricCheckin>.Forbidden("Você não tem permissão para atualizar este check-in.");
        }

        if (!tenantProvider.IsGlobalAdmin && tenantProvider.CollaboratorId != checkin.CollaboratorId)
        {
            return ServiceResult<MetricCheckin>.Forbidden("Apenas o autor pode editar este check-in.");
        }

        return await checkinService.UpdateAsync(id, request, cancellationToken);
    }

    public async Task<ServiceResult> DeleteAsync(
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var checkin = await entityLookup.GetMetricCheckinAsync(id, ignoreQueryFilters: true, cancellationToken: cancellationToken);

        if (checkin is null)
        {
            return ServiceResult.NotFound("Check-in não encontrado.");
        }

        var hasTenantAccess = await authorizationGateway.CanAccessTenantOrganizationAsync(user, checkin.OrganizationId, cancellationToken);
        if (!hasTenantAccess)
        {
            return ServiceResult.Forbidden("Você não tem permissão para excluir este check-in.");
        }

        if (!tenantProvider.IsGlobalAdmin && tenantProvider.CollaboratorId != checkin.CollaboratorId)
        {
            return ServiceResult.Forbidden("Apenas o autor pode excluir este check-in.");
        }

        return await checkinService.DeleteAsync(id, cancellationToken);
    }
}
