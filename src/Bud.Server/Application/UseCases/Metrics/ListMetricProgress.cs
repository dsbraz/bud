using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Domain.ReadModels;
using Bud.Server.Application.Ports;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.UseCases.Metrics;

public sealed class ListMetricProgress(IMissionProgressService missionProgressService)
{
    public async Task<Result<List<MetricProgressResponse>>> ExecuteAsync(
        List<Guid> metricIds,
        CancellationToken cancellationToken = default)
    {
        var result = await missionProgressService.GetMetricProgressAsync(metricIds, cancellationToken);
        if (!result.IsSuccess)
        {
            return Result<List<MetricProgressResponse>>.Failure(
                result.Error ?? "Falha ao calcular progresso das métricas.",
                result.ErrorType);
        }

        return Result<List<MetricProgressResponse>>.Success(result.Value!.Select(progress => progress.ToResponse()).ToList());
    }
}
