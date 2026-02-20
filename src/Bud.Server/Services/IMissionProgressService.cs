using Bud.Server.Domain.ReadModels;

namespace Bud.Server.Services;

public interface IMissionProgressService
{
    Task<ServiceResult<List<MissionProgressSnapshot>>> GetProgressAsync(List<Guid> missionIds, CancellationToken cancellationToken = default);
    Task<ServiceResult<List<MetricProgressSnapshot>>> GetMetricProgressAsync(List<Guid> metricIds, CancellationToken cancellationToken = default);
    Task<ServiceResult<List<ObjectiveProgressSnapshot>>> GetObjectiveProgressAsync(List<Guid> objectiveIds, CancellationToken cancellationToken = default);
}
