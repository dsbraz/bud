using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Application.Ports;

public interface IMissionObjectiveRepository
{
    Task<MissionObjective?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<MissionObjective?> GetByIdTrackedAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<MissionObjective>> GetByMissionAsync(Guid missionId, int page, int pageSize, CancellationToken ct = default);
    Task<Mission?> GetMissionAsync(Guid missionId, CancellationToken ct = default);
    Task<bool> DimensionBelongsToOrganizationAsync(Guid dimensionId, Guid organizationId, CancellationToken ct = default);
    Task AddAsync(MissionObjective entity, CancellationToken ct = default);
    Task RemoveAsync(MissionObjective entity, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
