using Bud.Server.Services;
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

    public Task<ServiceResult<List<MetricProgressDto>>> GetProgressAsync(
        List<Guid> metricIds,
        CancellationToken cancellationToken = default)
        => missionProgressService.GetMetricProgressAsync(metricIds, cancellationToken);
}
