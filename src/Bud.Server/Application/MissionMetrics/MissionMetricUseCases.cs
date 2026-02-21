using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.Projections;
using Bud.Server.Authorization;
using Bud.Server.Domain.Abstractions;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Server.Infrastructure.Services;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.MissionMetrics;

public sealed class DefineMissionMetric(
    IMissionMetricRepository metricRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<MissionMetric>> ExecuteAsync(
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
            var type = request.Type.ToDomain();
            var quantitativeType = request.QuantitativeType.ToDomain();
            var unit = request.Unit.ToDomain();

            var metric = MissionMetric.Create(
                Guid.NewGuid(),
                mission.OrganizationId,
                request.MissionId,
                request.Name,
                type);

            metric.ApplyTarget(type, quantitativeType, request.MinValue, request.MaxValue, unit, request.TargetText);

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
                        "Objetivo deve pertencer à mesma missão.",
                        ErrorType.Validation);
                }

                metric.MissionObjectiveId = request.MissionObjectiveId.Value;
            }

            await metricRepository.AddAsync(metric, cancellationToken);
            await unitOfWork.CommitAsync(metricRepository.SaveChangesAsync, cancellationToken);

            return Result<MissionMetric>.Success(metric);
        }
        catch (DomainInvariantException ex)
        {
            return Result<MissionMetric>.Failure(ex.Message, ErrorType.Validation);
        }
    }
}

public sealed class ReviseMissionMetricDefinition(
    IMissionMetricRepository metricRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<MissionMetric>> ExecuteAsync(
        ClaimsPrincipal user,
        Guid id,
        UpdateMissionMetricRequest request,
        CancellationToken cancellationToken = default)
    {
        var metricForAuthorization = await metricRepository.GetByIdAsync(id, cancellationToken);

        if (metricForAuthorization is null)
        {
            return Result<MissionMetric>.NotFound("Métrica da missão não encontrada.");
        }

        var canUpdate = await authorizationGateway.CanAccessTenantOrganizationAsync(
            user,
            metricForAuthorization.OrganizationId,
            cancellationToken);
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
            var type = request.Type.ToDomain();
            var quantitativeType = request.QuantitativeType.ToDomain();
            var unit = request.Unit.ToDomain();

            metric.UpdateDefinition(request.Name, type);
            metric.ApplyTarget(type, quantitativeType, request.MinValue, request.MaxValue, unit, request.TargetText);

            await unitOfWork.CommitAsync(metricRepository.SaveChangesAsync, cancellationToken);

            return Result<MissionMetric>.Success(metric);
        }
        catch (DomainInvariantException ex)
        {
            return Result<MissionMetric>.Failure(ex.Message, ErrorType.Validation);
        }
    }
}

public sealed class RemoveMissionMetric(
    IMissionMetricRepository metricRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result> ExecuteAsync(
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var metricForAuthorization = await metricRepository.GetByIdAsync(id, cancellationToken);

        if (metricForAuthorization is null)
        {
            return Result.NotFound("Métrica da missão não encontrada.");
        }

        var canDelete = await authorizationGateway.CanAccessTenantOrganizationAsync(
            user,
            metricForAuthorization.OrganizationId,
            cancellationToken);
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
        await unitOfWork.CommitAsync(metricRepository.SaveChangesAsync, cancellationToken);

        return Result.Success();
    }
}

public sealed class ViewMissionMetricDetails(IMissionMetricRepository metricRepository)
{
    public async Task<Result<MissionMetric>> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var metric = await metricRepository.GetByIdAsync(id, cancellationToken);
        return metric is null
            ? Result<MissionMetric>.NotFound("Métrica da missão não encontrada.")
            : Result<MissionMetric>.Success(metric);
    }
}

public sealed class BrowseMissionMetrics(IMissionMetricRepository metricRepository)
{
    public async Task<Result<PagedResult<MissionMetric>>> ExecuteAsync(
        Guid? missionId,
        Guid? objectiveId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);
        var result = await metricRepository.GetAllAsync(missionId, objectiveId, search, page, pageSize, cancellationToken);
        return Result<PagedResult<MissionMetric>>.Success(result);
    }
}

public sealed class CalculateMissionMetricProgress(IMissionProgressService missionProgressService)
{
    public async Task<Result<List<MetricProgressDto>>> ExecuteAsync(
        List<Guid> metricIds,
        CancellationToken cancellationToken = default)
    {
        var result = await missionProgressService.GetMetricProgressAsync(metricIds, cancellationToken);
        if (!result.IsSuccess)
        {
            return Result<List<MetricProgressDto>>.Failure(
                result.Error ?? "Falha ao calcular progresso das métricas.",
                result.ErrorType);
        }

        return Result<List<MetricProgressDto>>.Success(result.Value!.Select(progress => progress.ToContract()).ToList());
    }
}
