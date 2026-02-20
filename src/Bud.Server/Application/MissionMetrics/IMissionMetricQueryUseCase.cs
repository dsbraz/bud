using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using Bud.Server.Application.Common;

namespace Bud.Server.Application.MissionMetrics;

public interface IMissionMetricQueryUseCase
{
    Task<Result<MissionMetric>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<PagedResult<MissionMetric>>> GetAllAsync(
        Guid? missionId,
        Guid? objectiveId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Result<List<MetricProgressDto>>> GetProgressAsync(
        List<Guid> metricIds,
        CancellationToken cancellationToken = default);
}
