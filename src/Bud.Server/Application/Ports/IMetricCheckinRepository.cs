using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Application.Ports;

public interface IMetricCheckinRepository
{
    Task<MetricCheckin?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<MetricCheckin>> GetAllAsync(Guid? missionMetricId, Guid? missionId, int page, int pageSize, CancellationToken ct = default);
    Task<MissionMetric?> GetMetricWithMissionAsync(Guid metricId, CancellationToken ct = default);
    Task<MissionMetric?> GetMetricByIdAsync(Guid metricId, CancellationToken ct = default);
    Task AddAsync(MetricCheckin entity, CancellationToken ct = default);
    Task RemoveAsync(MetricCheckin entity, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
