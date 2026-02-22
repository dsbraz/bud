using Bud.Server.Domain.Model;

namespace Bud.Server.Domain.Repositories;

public interface IObjectiveRepository
{
    Task<Objective?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Objective?> GetByIdTrackedAsync(Guid id, CancellationToken ct = default);
    Task<Bud.Shared.Contracts.Common.PagedResult<Objective>> GetAllAsync(
        Guid? missionId,
        int page,
        int pageSize,
        CancellationToken ct = default);
    Task AddAsync(Objective entity, CancellationToken ct = default);
    Task RemoveAsync(Objective entity, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
