using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Authorization;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Shared.Contracts;
using Microsoft.Extensions.Logging;

namespace Bud.Server.Application.UseCases.Metrics;

public sealed partial class PatchMetric(
    IMetricRepository metricRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    ILogger<PatchMetric> logger,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<Metric>> ExecuteAsync(
        ClaimsPrincipal user,
        Guid id,
        PatchMetricRequest request,
        CancellationToken cancellationToken = default)
    {
        LogPatchingMetric(logger, id);

        var metricForAuthorization = await metricRepository.GetByIdAsync(id, cancellationToken);

        if (metricForAuthorization is null)
        {
            LogMetricPatchFailed(logger, id, "Not found");
            return Result<Metric>.NotFound("Métrica da missão não encontrada.");
        }

        var canUpdate = await authorizationGateway.CanAccessTenantOrganizationAsync(
            user,
            metricForAuthorization.OrganizationId,
            cancellationToken);
        if (!canUpdate)
        {
            LogMetricPatchFailed(logger, id, "Forbidden");
            return Result<Metric>.Forbidden("Você não tem permissão para atualizar métricas nesta missão.");
        }

        var metric = await metricRepository.GetByIdForUpdateAsync(id, cancellationToken);
        if (metric is null)
        {
            LogMetricPatchFailed(logger, id, "Not found for update");
            return Result<Metric>.NotFound("Métrica da missão não encontrada.");
        }

        try
        {
            var type = request.Type.HasValue ? request.Type.Value : metric.Type;
            var quantitativeType = request.QuantitativeType.HasValue
                ? request.QuantitativeType.Value
                : metric.QuantitativeType;
            var unit = request.Unit.HasValue ? request.Unit.Value : metric.Unit;
            var name = request.Name.HasValue ? (request.Name.Value ?? metric.Name) : metric.Name;
            var minValue = request.MinValue.HasValue ? request.MinValue.Value : metric.MinValue;
            var maxValue = request.MaxValue.HasValue ? request.MaxValue.Value : metric.MaxValue;
            var targetText = request.TargetText.HasValue ? request.TargetText.Value : metric.TargetText;

            metric.UpdateDefinition(name, type);
            metric.ApplyTarget(type, quantitativeType, minValue, maxValue, unit, targetText);

            await unitOfWork.CommitAsync(metricRepository.SaveChangesAsync, cancellationToken);

            LogMetricPatched(logger, id, metric.Name);
            return Result<Metric>.Success(metric);
        }
        catch (DomainInvariantException ex)
        {
            LogMetricPatchFailed(logger, id, ex.Message);
            return Result<Metric>.Failure(ex.Message, ErrorType.Validation);
        }
    }

    [LoggerMessage(EventId = 4057, Level = LogLevel.Information, Message = "Patching metric {MetricId}")]
    private static partial void LogPatchingMetric(ILogger logger, Guid metricId);

    [LoggerMessage(EventId = 4058, Level = LogLevel.Information, Message = "Metric patched successfully: {MetricId} - '{Name}'")]
    private static partial void LogMetricPatched(ILogger logger, Guid metricId, string name);

    [LoggerMessage(EventId = 4059, Level = LogLevel.Warning, Message = "Metric patch failed for {MetricId}: {Reason}")]
    private static partial void LogMetricPatchFailed(ILogger logger, Guid metricId, string reason);
}
