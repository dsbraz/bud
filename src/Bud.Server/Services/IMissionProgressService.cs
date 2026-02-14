using Bud.Shared.Contracts;

namespace Bud.Server.Services;

public interface IMissionProgressService
{
    Task<ServiceResult<List<MissionProgressDto>>> GetProgressAsync(List<Guid> missionIds, CancellationToken cancellationToken = default);
    Task<ServiceResult<List<MetricProgressDto>>> GetMetricProgressAsync(List<Guid> metricIds, CancellationToken cancellationToken = default);
    Task<ServiceResult<List<ObjectiveProgressDto>>> GetObjectiveProgressAsync(List<Guid> objectiveIds, CancellationToken cancellationToken = default);
}
