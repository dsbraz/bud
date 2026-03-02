using Bud.Server.Domain.Model;

namespace Bud.Server.Domain.Repositories;

public interface ITaskRepository
{
    Task<GoalTask?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Goal?> GetGoalByIdAsync(Guid goalId, CancellationToken ct = default);
    Task<Bud.Shared.Contracts.Common.PagedResult<GoalTask>> GetByGoalIdAsync(Guid goalId, int page, int pageSize, CancellationToken ct = default);
    Task<List<GoalTask>> GetActiveTasksForGoalsAsync(List<Guid> goalIds, CancellationToken ct = default);
    Task AddAsync(GoalTask entity, CancellationToken ct = default);
    Task RemoveAsync(GoalTask entity, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
