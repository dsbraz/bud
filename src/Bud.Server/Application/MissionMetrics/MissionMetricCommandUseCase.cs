using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.Ports;
using Bud.Server.Authorization;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Application.MissionMetrics;

public sealed class MissionMetricCommandUseCase(
    IMissionMetricRepository metricRepository,
    IApplicationAuthorizationGateway authorizationGateway) : IMissionMetricCommandUseCase
{
    public async Task<Result<MissionMetric>> CreateAsync(
        ClaimsPrincipal user,
        CreateMissionMetricRequest request,
        CancellationToken cancellationToken = default)
    {
        var mission = await metricRepository.GetMissionByIdAsync(request.MissionId, cancellationToken);

        if (mission is null)
        {
            return Result<MissionMetric>.NotFound("Missão não encontrada.");
        }

        var canCreate = await authorizationGateway.CanAccessTenantOrganizationAsync(user, mission.OrganizationId, cancellationToken);
        if (!canCreate)
        {
            return Result<MissionMetric>.Forbidden("Você não tem permissão para criar métricas nesta missão.");
        }

        try
        {
            var metric = MissionMetric.Create(
                Guid.NewGuid(),
                mission.OrganizationId,
                request.MissionId,
                request.Name,
                request.Type);

            metric.ApplyTarget(request.Type, request.QuantitativeType, request.MinValue, request.MaxValue, request.Unit, request.TargetText);

            if (request.MissionObjectiveId.HasValue)
            {
                var objective = await metricRepository.GetObjectiveByIdAsync(request.MissionObjectiveId.Value, cancellationToken);

                if (objective is null)
                {
                    return Result<MissionMetric>.NotFound("Objetivo não encontrado.");
                }

                if (objective.MissionId != request.MissionId)
                {
                    return Result<MissionMetric>.Failure(
                        "Objetivo deve pertencer à mesma missão.", ErrorType.Validation);
                }

                metric.MissionObjectiveId = request.MissionObjectiveId.Value;
            }

            await metricRepository.AddAsync(metric, cancellationToken);
            await metricRepository.SaveChangesAsync(cancellationToken);

            return Result<MissionMetric>.Success(metric);
        }
        catch (DomainInvariantException ex)
        {
            return Result<MissionMetric>.Failure(ex.Message, ErrorType.Validation);
        }
    }

    public async Task<Result<MissionMetric>> UpdateAsync(
        ClaimsPrincipal user,
        Guid id,
        UpdateMissionMetricRequest request,
        CancellationToken cancellationToken = default)
    {
        var metricForAuth = await metricRepository.GetByIdAsync(id, cancellationToken);

        if (metricForAuth is null)
        {
            return Result<MissionMetric>.NotFound("Métrica da missão não encontrada.");
        }

        var canUpdate = await authorizationGateway.CanAccessTenantOrganizationAsync(user, metricForAuth.OrganizationId, cancellationToken);
        if (!canUpdate)
        {
            return Result<MissionMetric>.Forbidden("Você não tem permissão para atualizar métricas nesta missão.");
        }

        var metric = await metricRepository.GetByIdTrackingAsync(id, cancellationToken);
        if (metric is null)
        {
            return Result<MissionMetric>.NotFound("Métrica da missão não encontrada.");
        }

        try
        {
            metric.UpdateDefinition(request.Name, request.Type);
            metric.ApplyTarget(request.Type, request.QuantitativeType, request.MinValue, request.MaxValue, request.Unit, request.TargetText);

            await metricRepository.SaveChangesAsync(cancellationToken);

            return Result<MissionMetric>.Success(metric);
        }
        catch (DomainInvariantException ex)
        {
            return Result<MissionMetric>.Failure(ex.Message, ErrorType.Validation);
        }
    }

    public async Task<Result> DeleteAsync(
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var metricForAuth = await metricRepository.GetByIdAsync(id, cancellationToken);

        if (metricForAuth is null)
        {
            return Result.NotFound("Métrica da missão não encontrada.");
        }

        var canDelete = await authorizationGateway.CanAccessTenantOrganizationAsync(user, metricForAuth.OrganizationId, cancellationToken);
        if (!canDelete)
        {
            return Result.Forbidden("Você não tem permissão para excluir métricas nesta missão.");
        }

        var metric = await metricRepository.GetByIdTrackingAsync(id, cancellationToken);
        if (metric is null)
        {
            return Result.NotFound("Métrica da missão não encontrada.");
        }

        await metricRepository.RemoveAsync(metric, cancellationToken);
        await metricRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
