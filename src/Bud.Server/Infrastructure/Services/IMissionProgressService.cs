using Bud.Server.Application.Projections;
using Bud.Server.Application.Common;

namespace Bud.Server.Infrastructure.Services;

public interface IMissionProgressService
{
    Task<Result<List<MissionProgressSnapshot>>> GetProgressAsync(List<Guid> missionIds, CancellationToken cancellationToken = default);
    Task<Result<List<MetricProgressSnapshot>>> GetMetricProgressAsync(List<Guid> metricIds, CancellationToken cancellationToken = default);
    Task<Result<List<ObjectiveProgressSnapshot>>> GetObjectiveProgressAsync(List<Guid> objectiveIds, CancellationToken cancellationToken = default);
}
