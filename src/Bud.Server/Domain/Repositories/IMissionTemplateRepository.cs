using Bud.Server.Domain.Model;

namespace Bud.Server.Domain.Repositories;

public interface IMissionTemplateRepository
{
    Task<MissionTemplate?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<MissionTemplate?> GetByIdWithChildrenAsync(Guid id, CancellationToken ct = default);
    Task<MissionTemplate?> GetByIdReadOnlyAsync(Guid id, CancellationToken ct = default);
    Task<Bud.Shared.Contracts.PagedResult<MissionTemplate>> GetAllAsync(string? search, int page, int pageSize, CancellationToken ct = default);
    Task AddAsync(MissionTemplate entity, CancellationToken ct = default);
    Task RemoveAsync(MissionTemplate entity, CancellationToken ct = default);
    Task RemoveObjectivesAndMetrics(IEnumerable<MissionTemplateObjective> objectives, IEnumerable<MissionTemplateMetric> metrics, CancellationToken ct = default);
    Task AddObjectivesAndMetrics(IEnumerable<MissionTemplateObjective> objectives, IEnumerable<MissionTemplateMetric> metrics, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
