using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Authorization;
using Bud.Server.Domain.Repositories;
using Bud.Server.MultiTenancy;
using Microsoft.Extensions.Logging;

namespace Bud.Server.Application.UseCases.Metrics;

public sealed partial class DeleteMetricCheckin(
    IMetricRepository metricRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    ITenantProvider tenantProvider,
    ILogger<DeleteMetricCheckin> logger,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result> ExecuteAsync(
        ClaimsPrincipal user,
        Guid metricId,
        Guid checkinId,
        CancellationToken cancellationToken = default)
    {
        LogDeletingMetricCheckin(logger, checkinId, metricId);

        var checkin = await metricRepository.GetCheckinByIdAsync(checkinId, cancellationToken);
        if (checkin is null || checkin.MetricId != metricId)
        {
            LogMetricCheckinDeletionFailed(logger, checkinId, "Not found");
            return Result.NotFound("Check-in não encontrado.");
        }

        var hasTenantAccess = await authorizationGateway.CanAccessTenantOrganizationAsync(user, checkin.OrganizationId, cancellationToken);
        if (!hasTenantAccess)
        {
            LogMetricCheckinDeletionFailed(logger, checkinId, "Forbidden (tenant)");
            return Result.Forbidden("Você não tem permissão para excluir este check-in.");
        }

        if (!tenantProvider.IsGlobalAdmin && tenantProvider.CollaboratorId != checkin.CollaboratorId)
        {
            LogMetricCheckinDeletionFailed(logger, checkinId, "Not the author");
            return Result.Forbidden("Apenas o autor pode excluir este check-in.");
        }

        await metricRepository.RemoveCheckinAsync(checkin, cancellationToken);
        await unitOfWork.CommitAsync(metricRepository.SaveChangesAsync, cancellationToken);

        LogMetricCheckinDeleted(logger, checkinId, metricId);
        return Result.Success();
    }

    [LoggerMessage(EventId = 4069, Level = LogLevel.Information, Message = "Deleting checkin {CheckinId} for metric {MetricId}")]
    private static partial void LogDeletingMetricCheckin(ILogger logger, Guid checkinId, Guid metricId);

    [LoggerMessage(EventId = 4070, Level = LogLevel.Information, Message = "Metric checkin deleted successfully: {CheckinId} for metric {MetricId}")]
    private static partial void LogMetricCheckinDeleted(ILogger logger, Guid checkinId, Guid metricId);

    [LoggerMessage(EventId = 4071, Level = LogLevel.Warning, Message = "Metric checkin deletion failed for {CheckinId}: {Reason}")]
    private static partial void LogMetricCheckinDeletionFailed(ILogger logger, Guid checkinId, string reason);
}
