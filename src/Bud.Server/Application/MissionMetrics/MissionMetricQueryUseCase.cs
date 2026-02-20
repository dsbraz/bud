using Bud.Server.Services;
using Bud.Server.Application.Common;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Application.MissionMetrics;

public sealed class MissionMetricQueryUseCase(
    IMissionMetricService metricService,
    IMissionProgressService missionProgressService) : IMissionMetricQueryUseCase
{
    public Task<ServiceResult<MissionMetric>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => metricService.GetByIdAsync(id, cancellationToken);

    public Task<ServiceResult<PagedResult<MissionMetric>>> GetAllAsync(
        Guid? missionId,
        Guid? objectiveId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
        => metricService.GetAllAsync(missionId, objectiveId, search, page, pageSize, cancellationToken);

    public async Task<ServiceResult<List<MetricProgressDto>>> GetProgressAsync(
        List<Guid> metricIds,
        CancellationToken cancellationToken = default)
    {
        var result = await missionProgressService.GetMetricProgressAsync(metricIds, cancellationToken);
        if (!result.IsSuccess)
        {
            return ServiceResult<List<MetricProgressDto>>.Failure(result.Error ?? "Falha ao calcular progresso das métricas.", result.ErrorType);
        }

        return ServiceResult<List<MetricProgressDto>>.Success(
            result.Value!.Select(p => p.ToContract()).ToList());
    }
}
