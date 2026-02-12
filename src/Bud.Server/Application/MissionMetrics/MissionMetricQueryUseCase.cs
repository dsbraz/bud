using Bud.Server.Application.Abstractions;
using Bud.Shared.Contracts;
using Bud.Shared.Models;

namespace Bud.Server.Application.MissionMetrics;

public sealed class MissionMetricQueryUseCase(
    IMissionMetricQueryService metricService,
    IMissionProgressService missionProgressService) : IMissionMetricQueryUseCase
{
    public Task<ServiceResult<MissionMetric>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => metricService.GetByIdAsync(id, cancellationToken);

    public Task<ServiceResult<PagedResult<MissionMetric>>> GetAllAsync(
        Guid? missionId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
        => metricService.GetAllAsync(missionId, search, page, pageSize, cancellationToken);

    public Task<ServiceResult<List<MetricProgressDto>>> GetProgressAsync(
        List<Guid> metricIds,
        CancellationToken cancellationToken = default)
        => missionProgressService.GetMetricProgressAsync(metricIds, cancellationToken);
}
