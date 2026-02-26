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

public sealed partial class PatchMetricCheckin(
    IMetricRepository metricRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    ITenantProvider tenantProvider,
    ILogger<PatchMetricCheckin> logger,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<MetricCheckin>> ExecuteAsync(
        ClaimsPrincipal user,
        Guid metricId,
        Guid checkinId,
        PatchCheckinRequest request,
        CancellationToken cancellationToken = default)
    {
        LogPatchingMetricCheckin(logger, checkinId, metricId);

        var checkin = await metricRepository.GetCheckinByIdAsync(checkinId, cancellationToken);
        if (checkin is null || checkin.MetricId != metricId)
        {
            LogMetricCheckinPatchFailed(logger, checkinId, "Not found");
            return Result<MetricCheckin>.NotFound("Check-in não encontrado.");
        }

        var hasTenantAccess = await authorizationGateway.CanAccessTenantOrganizationAsync(user, checkin.OrganizationId, cancellationToken);
        if (!hasTenantAccess)
        {
            LogMetricCheckinPatchFailed(logger, checkinId, "Forbidden (tenant)");
            return Result<MetricCheckin>.Forbidden("Você não tem permissão para atualizar este check-in.");
        }

        if (!tenantProvider.IsGlobalAdmin && tenantProvider.CollaboratorId != checkin.CollaboratorId)
        {
            LogMetricCheckinPatchFailed(logger, checkinId, "Not the author");
            return Result<MetricCheckin>.Forbidden("Apenas o autor pode editar este check-in.");
        }

        var metric = await metricRepository.GetByIdAsync(metricId, cancellationToken);
        if (metric is null)
        {
            LogMetricCheckinPatchFailed(logger, checkinId, "Metric not found");
            return Result<MetricCheckin>.NotFound("Métrica não encontrada.");
        }

        try
        {
            metric.UpdateCheckin(
                checkin,
                request.Value,
                request.Text,
                DateTime.SpecifyKind(request.CheckinDate, DateTimeKind.Utc),
                request.Note,
                request.ConfidenceLevel);

            await unitOfWork.CommitAsync(metricRepository.SaveChangesAsync, cancellationToken);
            LogMetricCheckinPatched(logger, checkinId, metricId);
            return Result<MetricCheckin>.Success(checkin);
        }
        catch (DomainInvariantException ex)
        {
            LogMetricCheckinPatchFailed(logger, checkinId, ex.Message);
            return Result<MetricCheckin>.Failure(ex.Message, ErrorType.Validation);
        }
    }

    [LoggerMessage(EventId = 4066, Level = LogLevel.Information, Message = "Patching checkin {CheckinId} for metric {MetricId}")]
    private static partial void LogPatchingMetricCheckin(ILogger logger, Guid checkinId, Guid metricId);

    [LoggerMessage(EventId = 4067, Level = LogLevel.Information, Message = "Metric checkin patched successfully: {CheckinId} for metric {MetricId}")]
    private static partial void LogMetricCheckinPatched(ILogger logger, Guid checkinId, Guid metricId);

    [LoggerMessage(EventId = 4068, Level = LogLevel.Warning, Message = "Metric checkin patch failed for {CheckinId}: {Reason}")]
    private static partial void LogMetricCheckinPatchFailed(ILogger logger, Guid checkinId, string reason);
}
