using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Application.Ports;

public interface IMissionMetricRepository
{
    Task<MissionMetric?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<MissionMetric?> GetByIdTrackingAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<MissionMetric>> GetAllAsync(Guid? missionId, Guid? objectiveId, string? search, int page, int pageSize, CancellationToken ct = default);
    Task<Mission?> GetMissionByIdAsync(Guid missionId, CancellationToken ct = default);
    Task<MissionObjective?> GetObjectiveByIdAsync(Guid objectiveId, CancellationToken ct = default);
    Task AddAsync(MissionMetric entity, CancellationToken ct = default);
    Task RemoveAsync(MissionMetric entity, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
