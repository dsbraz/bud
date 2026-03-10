using Bud.Application.Common;
using Bud.Infrastructure.Persistence;
using Bud.Shared.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Bud.Infrastructure.Features.Goals;

public sealed class TaskRepository(ApplicationDbContext dbContext) : ITaskRepository
{
    public async Task<GoalTask?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await dbContext.GoalTasks.FindAsync([id], ct);

    public async Task<Goal?> GetGoalByIdAsync(Guid goalId, CancellationToken ct = default)
        => await dbContext.Goals
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == goalId, ct);

    public async Task<PagedResult<GoalTask>> GetByGoalIdAsync(Guid goalId, int page, int pageSize, CancellationToken ct = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);

        var query = dbContext.GoalTasks
            .AsNoTracking()
            .Where(t => t.GoalId == goalId);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(t => t.State)
            .ThenBy(t => t.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<GoalTask>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<List<GoalTask>> GetActiveTasksForGoalsAsync(List<Guid> goalIds, CancellationToken ct = default)
        => await dbContext.GoalTasks
            .AsNoTracking()
            .Where(t => goalIds.Contains(t.GoalId)
                && (t.State == TaskState.ToDo || t.State == TaskState.Doing))
            .OrderBy(t => t.State)
            .ThenBy(t => t.Name)
            .ToListAsync(ct);

    public async Task AddAsync(GoalTask entity, CancellationToken ct = default)
        => await dbContext.GoalTasks.AddAsync(entity, ct);

    public Task RemoveAsync(GoalTask entity, CancellationToken ct = default)
    {
        dbContext.GoalTasks.Remove(entity);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await dbContext.SaveChangesAsync(ct);
}
