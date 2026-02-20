using Bud.Server.Domain.ReadModels;
using Bud.Server.Application.Common;

namespace Bud.Server.Application.Ports;

public interface IMissionProgressService
{
    Task<Result<List<MissionProgressSnapshot>>> GetProgressAsync(List<Guid> missionIds, CancellationToken cancellationToken = default);
    Task<Result<List<MetricProgressSnapshot>>> GetMetricProgressAsync(List<Guid> metricIds, CancellationToken cancellationToken = default);
    Task<Result<List<ObjectiveProgressSnapshot>>> GetObjectiveProgressAsync(List<Guid> objectiveIds, CancellationToken cancellationToken = default);
}
