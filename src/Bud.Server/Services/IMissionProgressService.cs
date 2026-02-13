using Bud.Shared.Contracts;

namespace Bud.Server.Application.Abstractions;

public interface IMissionProgressService
{
    Task<ServiceResult<List<MissionProgressDto>>> GetProgressAsync(List<Guid> missionIds, CancellationToken cancellationToken = default);
    Task<ServiceResult<List<MetricProgressDto>>> GetMetricProgressAsync(List<Guid> metricIds, CancellationToken cancellationToken = default);
}
