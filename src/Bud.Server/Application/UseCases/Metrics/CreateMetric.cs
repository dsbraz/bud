using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Authorization;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Shared.Contracts;
using Microsoft.Extensions.Logging;

namespace Bud.Server.Application.UseCases.Metrics;

public sealed partial class CreateMetric(
    IMetricRepository metricRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    ILogger<CreateMetric> logger,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<Metric>> ExecuteAsync(
        ClaimsPrincipal user,
        CreateMetricRequest request,
        CancellationToken cancellationToken = default)
    {
        LogCreatingMetric(logger, request.Name, request.MissionId);

        var mission = await metricRepository.GetMissionByIdAsync(request.MissionId, cancellationToken);

        if (mission is null)
        {
            LogMetricCreationFailed(logger, request.Name, "Mission not found");
            return Result<Metric>.NotFound("Missão não encontrada.");
        }

        var canCreate = await authorizationGateway.CanAccessTenantOrganizationAsync(user, mission.OrganizationId, cancellationToken);
        if (!canCreate)
        {
            LogMetricCreationFailed(logger, request.Name, "Forbidden");
            return Result<Metric>.Forbidden("Você não tem permissão para criar métricas nesta missão.");
        }

        try
        {
            var type = request.Type;
            var quantitativeType = request.QuantitativeType;
            var unit = request.Unit;

            var metric = Metric.Create(
                Guid.NewGuid(),
                mission.OrganizationId,
                request.MissionId,
                request.Name,
                type);

            metric.ApplyTarget(type, quantitativeType, request.MinValue, request.MaxValue, unit, request.TargetText);

            if (request.ObjectiveId.HasValue)
            {
                var objective = await metricRepository.GetObjectiveByIdAsync(request.ObjectiveId.Value, cancellationToken);

                if (objective is null)
                {
                    LogMetricCreationFailed(logger, request.Name, "Objective not found");
                    return Result<Metric>.NotFound("Objetivo não encontrado.");
                }

                if (objective.MissionId != request.MissionId)
                {
                    LogMetricCreationFailed(logger, request.Name, "Objective belongs to different mission");
                    return Result<Metric>.Failure(
                        "Objetivo deve pertencer à mesma missão.",
                        ErrorType.Validation);
                }

                metric.ObjectiveId = request.ObjectiveId.Value;
            }

            await metricRepository.AddAsync(metric, cancellationToken);
            await unitOfWork.CommitAsync(metricRepository.SaveChangesAsync, cancellationToken);

            LogMetricCreated(logger, metric.Id, metric.Name);
            return Result<Metric>.Success(metric);
        }
        catch (DomainInvariantException ex)
        {
            LogMetricCreationFailed(logger, request.Name, ex.Message);
            return Result<Metric>.Failure(ex.Message, ErrorType.Validation);
        }
    }

    [LoggerMessage(EventId = 4054, Level = LogLevel.Information, Message = "Creating metric '{Name}' for mission {MissionId}")]
    private static partial void LogCreatingMetric(ILogger logger, string name, Guid missionId);

    [LoggerMessage(EventId = 4055, Level = LogLevel.Information, Message = "Metric created successfully: {MetricId} - '{Name}'")]
    private static partial void LogMetricCreated(ILogger logger, Guid metricId, string name);

    [LoggerMessage(EventId = 4056, Level = LogLevel.Warning, Message = "Metric creation failed for '{Name}': {Reason}")]
    private static partial void LogMetricCreationFailed(ILogger logger, string name, string reason);
}
