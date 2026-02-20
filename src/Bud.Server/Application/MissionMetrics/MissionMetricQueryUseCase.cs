using Bud.Server.Application.Common;
using Bud.Server.Application.Ports;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Application.MissionMetrics;

public sealed class MissionMetricQueryUseCase(
    IMissionMetricRepository metricRepository,
    IMissionProgressService missionProgressService) : IMissionMetricQueryUseCase
{
    public async Task<Result<MissionMetric>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var metric = await metricRepository.GetByIdAsync(id, cancellationToken);
        return metric is null
            ? Result<MissionMetric>.NotFound("Métrica da missão não encontrada.")
            : Result<MissionMetric>.Success(metric);
    }

    public async Task<Result<PagedResult<MissionMetric>>> GetAllAsync(
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

    public async Task<Result<List<MetricProgressDto>>> GetProgressAsync(
        List<Guid> metricIds,
        CancellationToken cancellationToken = default)
    {
        var result = await missionProgressService.GetMetricProgressAsync(metricIds, cancellationToken);
        if (!result.IsSuccess)
        {
            return Result<List<MetricProgressDto>>.Failure(result.Error ?? "Falha ao calcular progresso das métricas.", result.ErrorType);
        }

        return Result<List<MetricProgressDto>>.Success(
            result.Value!.Select(p => p.ToContract()).ToList());
    }
}
