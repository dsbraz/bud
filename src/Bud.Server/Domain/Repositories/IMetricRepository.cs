using Bud.Server.Domain.Model;

namespace Bud.Server.Domain.Repositories;

public interface IMetricRepository
{
    Task<Metric?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Metric?> GetByIdTrackingAsync(Guid id, CancellationToken ct = default);
    Task<Bud.Shared.Contracts.Common.PagedResult<Metric>> GetAllAsync(Guid? missionId, Guid? objectiveId, string? search, int page, int pageSize, CancellationToken ct = default);
    Task<Mission?> GetMissionByIdAsync(Guid missionId, CancellationToken ct = default);
    Task<Objective?> GetObjectiveByIdAsync(Guid objectiveId, CancellationToken ct = default);
    Task<MetricCheckin?> GetCheckinByIdAsync(Guid id, CancellationToken ct = default);
    Task<Bud.Shared.Contracts.Common.PagedResult<MetricCheckin>> GetCheckinsAsync(Guid? metricId, Guid? missionId, int page, int pageSize, CancellationToken ct = default);
    Task<Metric?> GetMetricWithMissionAsync(Guid metricId, CancellationToken ct = default);
    Task AddCheckinAsync(MetricCheckin entity, CancellationToken ct = default);
    Task RemoveCheckinAsync(MetricCheckin entity, CancellationToken ct = default);
    Task AddAsync(Metric entity, CancellationToken ct = default);
    Task RemoveAsync(Metric entity, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
