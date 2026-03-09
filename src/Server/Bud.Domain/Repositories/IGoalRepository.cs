using Bud.Domain.Model;

namespace Bud.Domain.Repositories;

public interface IGoalRepository
{
    Task<Goal?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Goal?> GetByIdReadOnlyAsync(Guid id, CancellationToken ct = default);
    Task<Bud.Shared.Contracts.Common.PagedResult<Goal>> GetAllAsync(
        GoalFilter? filter, Guid? collaboratorId, string? search,
        int page, int pageSize, CancellationToken ct = default);
    Task<Bud.Shared.Contracts.Common.PagedResult<Goal>> GetChildrenAsync(Guid parentId, int page, int pageSize, CancellationToken ct = default);
    Task<Bud.Shared.Contracts.Common.PagedResult<Indicator>> GetIndicatorsAsync(Guid goalId, int page, int pageSize, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Goal entity, CancellationToken ct = default);
    Task RemoveAsync(Goal entity, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
