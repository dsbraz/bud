using System.Security.Claims;
using Bud.Server.Application.Abstractions;
using Bud.Server.Application.Common.Authorization;
using Bud.Server.Application.Common.Events;
using Bud.Server.Application.Common.Pipeline;
using Bud.Server.Application.Common.ReadModel;
using Bud.Server.Domain.MissionMetrics.Events;
using Bud.Shared.Contracts;
using Bud.Shared.Models;

namespace Bud.Server.Application.MissionMetrics;

public sealed class MissionMetricCommandUseCase(
    IMissionMetricCommandService metricService,
    IApplicationAuthorizationGateway authorizationGateway,
    IApplicationEntityLookup entityLookup,
    IUseCasePipeline? useCasePipeline = null,
    IDomainEventDispatcher? domainEventDispatcher = null) : IMissionMetricCommandUseCase
{
    private readonly IUseCasePipeline _useCasePipeline = useCasePipeline ?? NoOpUseCasePipeline.Instance;
    private readonly IDomainEventDispatcher _domainEventDispatcher = domainEventDispatcher ?? NoOpDomainEventDispatcher.Instance;

    public async Task<ServiceResult<MissionMetric>> CreateAsync(
        ClaimsPrincipal user,
        CreateMissionMetricRequest request,
        CancellationToken cancellationToken = default)
    {
        return await _useCasePipeline.ExecuteAsync(
            new UseCaseExecutionContext(nameof(MissionMetricCommandUseCase), nameof(CreateAsync)),
            async ct =>
            {
                var mission = await entityLookup.GetMissionAsync(request.MissionId, ignoreQueryFilters: true, cancellationToken: ct);

                if (mission is null)
                {
                    return ServiceResult<MissionMetric>.NotFound("Missão não encontrada.");
                }

                var canCreate = await authorizationGateway.CanAccessTenantOrganizationAsync(user, mission.OrganizationId, ct);
                if (!canCreate)
                {
                    return ServiceResult<MissionMetric>.Forbidden("Você não tem permissão para criar métricas nesta missão.");
                }

                var createResult = await metricService.CreateAsync(request, ct);
                if (createResult.IsSuccess)
                {
                    await _domainEventDispatcher.DispatchAsync(
                        new MissionMetricCreatedDomainEvent(
                            createResult.Value!.Id,
                            createResult.Value.MissionId,
                            createResult.Value.OrganizationId),
                        ct);
                }

                return createResult;
            },
            cancellationToken);
    }

    public async Task<ServiceResult<MissionMetric>> UpdateAsync(
        ClaimsPrincipal user,
        Guid id,
        UpdateMissionMetricRequest request,
        CancellationToken cancellationToken = default)
    {
        return await _useCasePipeline.ExecuteAsync(
            new UseCaseExecutionContext(nameof(MissionMetricCommandUseCase), nameof(UpdateAsync)),
            async ct =>
            {
                var metric = await entityLookup.GetMissionMetricAsync(id, ignoreQueryFilters: true, cancellationToken: ct);

                if (metric is null)
                {
                    return ServiceResult<MissionMetric>.NotFound("Métrica da missão não encontrada.");
                }

                var canUpdate = await authorizationGateway.CanAccessTenantOrganizationAsync(user, metric.OrganizationId, ct);
                if (!canUpdate)
                {
                    return ServiceResult<MissionMetric>.Forbidden("Você não tem permissão para atualizar métricas nesta missão.");
                }

                var result = await metricService.UpdateAsync(id, request, ct);
                if (result.IsSuccess)
                {
                    await _domainEventDispatcher.DispatchAsync(
                        new MissionMetricUpdatedDomainEvent(result.Value!.Id, result.Value.MissionId, result.Value.OrganizationId),
                        ct);
                }

                return result;
            },
            cancellationToken);
    }

    public async Task<ServiceResult> DeleteAsync(
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _useCasePipeline.ExecuteAsync(
            new UseCaseExecutionContext(nameof(MissionMetricCommandUseCase), nameof(DeleteAsync)),
            async ct =>
            {
                var metric = await entityLookup.GetMissionMetricAsync(id, ignoreQueryFilters: true, cancellationToken: ct);

                if (metric is null)
                {
                    return ServiceResult.NotFound("Métrica da missão não encontrada.");
                }

                var canDelete = await authorizationGateway.CanAccessTenantOrganizationAsync(user, metric.OrganizationId, ct);
                if (!canDelete)
                {
                    return ServiceResult.Forbidden("Você não tem permissão para excluir métricas nesta missão.");
                }

                var result = await metricService.DeleteAsync(id, ct);
                if (result.IsSuccess)
                {
                    await _domainEventDispatcher.DispatchAsync(
                        new MissionMetricDeletedDomainEvent(metric.Id, metric.MissionId, metric.OrganizationId),
                        ct);
                }

                return result;
            },
            cancellationToken);
    }
}
