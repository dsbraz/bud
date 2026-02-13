using System.Security.Claims;
using Bud.Server.Services;
using Bud.Server.Authorization;
using Bud.Server.Data;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Application.MissionMetrics;

public sealed class MissionMetricCommandUseCase(
    IMissionMetricService metricService,
    IApplicationAuthorizationGateway authorizationGateway,
    IApplicationEntityLookup entityLookup) : IMissionMetricCommandUseCase
{
    public async Task<ServiceResult<MissionMetric>> CreateAsync(
        ClaimsPrincipal user,
        CreateMissionMetricRequest request,
        CancellationToken cancellationToken = default)
    {
        var mission = await entityLookup.GetMissionAsync(request.MissionId, ignoreQueryFilters: true, cancellationToken: cancellationToken);

        if (mission is null)
        {
            return ServiceResult<MissionMetric>.NotFound("Missão não encontrada.");
        }

        var canCreate = await authorizationGateway.CanAccessTenantOrganizationAsync(user, mission.OrganizationId, cancellationToken);
        if (!canCreate)
        {
            return ServiceResult<MissionMetric>.Forbidden("Você não tem permissão para criar métricas nesta missão.");
        }

        return await metricService.CreateAsync(request, cancellationToken);
    }

    public async Task<ServiceResult<MissionMetric>> UpdateAsync(
        ClaimsPrincipal user,
        Guid id,
        UpdateMissionMetricRequest request,
        CancellationToken cancellationToken = default)
    {
        var metric = await entityLookup.GetMissionMetricAsync(id, ignoreQueryFilters: true, cancellationToken: cancellationToken);

        if (metric is null)
        {
            return ServiceResult<MissionMetric>.NotFound("Métrica da missão não encontrada.");
        }

        var canUpdate = await authorizationGateway.CanAccessTenantOrganizationAsync(user, metric.OrganizationId, cancellationToken);
        if (!canUpdate)
        {
            return ServiceResult<MissionMetric>.Forbidden("Você não tem permissão para atualizar métricas nesta missão.");
        }

        return await metricService.UpdateAsync(id, request, cancellationToken);
    }

    public async Task<ServiceResult> DeleteAsync(
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var metric = await entityLookup.GetMissionMetricAsync(id, ignoreQueryFilters: true, cancellationToken: cancellationToken);

        if (metric is null)
        {
            return ServiceResult.NotFound("Métrica da missão não encontrada.");
        }

        var canDelete = await authorizationGateway.CanAccessTenantOrganizationAsync(user, metric.OrganizationId, cancellationToken);
        if (!canDelete)
        {
            return ServiceResult.Forbidden("Você não tem permissão para excluir métricas nesta missão.");
        }

        return await metricService.DeleteAsync(id, cancellationToken);
    }
}
