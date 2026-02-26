using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Authorization;
using Bud.Server.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Bud.Server.Application.UseCases.Metrics;

public sealed partial class DeleteMetric(
    IMetricRepository metricRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    ILogger<DeleteMetric> logger,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result> ExecuteAsync(
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        LogDeletingMetric(logger, id);

        var metricForAuthorization = await metricRepository.GetByIdAsync(id, cancellationToken);

        if (metricForAuthorization is null)
        {
            LogMetricDeletionFailed(logger, id, "Not found");
            return Result.NotFound("Métrica da missão não encontrada.");
        }

        var canDelete = await authorizationGateway.CanAccessTenantOrganizationAsync(
            user,
            metricForAuthorization.OrganizationId,
            cancellationToken);
        if (!canDelete)
        {
            LogMetricDeletionFailed(logger, id, "Forbidden");
            return Result.Forbidden("Você não tem permissão para excluir métricas nesta missão.");
        }

        var metric = await metricRepository.GetByIdForUpdateAsync(id, cancellationToken);
        if (metric is null)
        {
            LogMetricDeletionFailed(logger, id, "Not found for update");
            return Result.NotFound("Métrica da missão não encontrada.");
        }

        await metricRepository.RemoveAsync(metric, cancellationToken);
        await unitOfWork.CommitAsync(metricRepository.SaveChangesAsync, cancellationToken);

        LogMetricDeleted(logger, id);
        return Result.Success();
    }

    [LoggerMessage(EventId = 4060, Level = LogLevel.Information, Message = "Deleting metric {MetricId}")]
    private static partial void LogDeletingMetric(ILogger logger, Guid metricId);

    [LoggerMessage(EventId = 4061, Level = LogLevel.Information, Message = "Metric deleted successfully: {MetricId}")]
    private static partial void LogMetricDeleted(ILogger logger, Guid metricId);

    [LoggerMessage(EventId = 4062, Level = LogLevel.Warning, Message = "Metric deletion failed for {MetricId}: {Reason}")]
    private static partial void LogMetricDeletionFailed(ILogger logger, Guid metricId, string reason);
}
