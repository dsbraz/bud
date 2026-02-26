using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Authorization;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Server.MultiTenancy;
using Bud.Shared.Contracts;
using Microsoft.Extensions.Logging;

namespace Bud.Server.Application.UseCases.Metrics;

public sealed partial class CreateMetricCheckin(
    IMetricRepository metricRepository,
    ICollaboratorRepository collaboratorRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    ITenantProvider tenantProvider,
    ILogger<CreateMetricCheckin> logger,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<MetricCheckin>> ExecuteAsync(
        ClaimsPrincipal user,
        Guid metricId,
        CreateCheckinRequest request,
        CancellationToken cancellationToken = default)
    {
        LogCreatingMetricCheckin(logger, metricId);

        var metric = await metricRepository.GetMetricWithMissionAsync(metricId, cancellationToken);
        if (metric is null)
        {
            LogMetricCheckinCreationFailed(logger, metricId, "Metric not found");
            return Result<MetricCheckin>.NotFound("Métrica não encontrada.");
        }

        var hasTenantAccess = await authorizationGateway.CanAccessTenantOrganizationAsync(user, metric.OrganizationId, cancellationToken);
        if (!hasTenantAccess)
        {
            LogMetricCheckinCreationFailed(logger, metricId, "Forbidden (tenant)");
            return Result<MetricCheckin>.Forbidden("Você não tem permissão para criar check-ins nesta métrica.");
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
            LogMetricCheckinCreationFailed(logger, metricId, "Forbidden (scope)");
            return Result<MetricCheckin>.Forbidden("Você não tem permissão para fazer check-in nesta métrica.");
        }

        var collaboratorId = tenantProvider.CollaboratorId;
        if (!collaboratorId.HasValue)
        {
            LogMetricCheckinCreationFailed(logger, metricId, "Collaborator not identified");
            return Result<MetricCheckin>.Forbidden("Colaborador não identificado.");
        }

        var collaborator = await collaboratorRepository.GetByIdAsync(collaboratorId.Value, cancellationToken);
        if (collaborator is null)
        {
            LogMetricCheckinCreationFailed(logger, metricId, "Collaborator not found");
            return Result<MetricCheckin>.Forbidden("Colaborador não encontrado.");
        }

        if (mission.Status != MissionStatus.Active)
        {
            LogMetricCheckinCreationFailed(logger, metricId, "Mission not active");
            return Result<MetricCheckin>.Failure(
                "Não é possível fazer check-in em métricas de missões que não estão ativas.",
                ErrorType.Validation);
        }

        try
        {
            var checkin = metric.CreateCheckin(
                Guid.NewGuid(),
                collaboratorId.Value,
                request.Value,
                request.Text,
                DateTime.SpecifyKind(request.CheckinDate, DateTimeKind.Utc),
                request.Note,
                request.ConfidenceLevel);

            await metricRepository.AddCheckinAsync(checkin, cancellationToken);
            await unitOfWork.CommitAsync(metricRepository.SaveChangesAsync, cancellationToken);

            LogMetricCheckinCreated(logger, checkin.Id, metricId);
            return Result<MetricCheckin>.Success(checkin);
        }
        catch (DomainInvariantException ex)
        {
            LogMetricCheckinCreationFailed(logger, metricId, ex.Message);
            return Result<MetricCheckin>.Failure(ex.Message, ErrorType.Validation);
        }
    }

    [LoggerMessage(EventId = 4063, Level = LogLevel.Information, Message = "Creating checkin for metric {MetricId}")]
    private static partial void LogCreatingMetricCheckin(ILogger logger, Guid metricId);

    [LoggerMessage(EventId = 4064, Level = LogLevel.Information, Message = "Metric checkin created successfully: {CheckinId} for metric {MetricId}")]
    private static partial void LogMetricCheckinCreated(ILogger logger, Guid checkinId, Guid metricId);

    [LoggerMessage(EventId = 4065, Level = LogLevel.Warning, Message = "Metric checkin creation failed for metric {MetricId}: {Reason}")]
    private static partial void LogMetricCheckinCreationFailed(ILogger logger, Guid metricId, string reason);
}
