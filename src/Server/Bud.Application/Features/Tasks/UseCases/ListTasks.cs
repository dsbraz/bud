using Bud.Application.Common;
using Bud.Application.Features.Goals;

namespace Bud.Application.Features.Tasks.UseCases;

public sealed class ListTasks(ITaskRepository taskRepository, IGoalRepository goalRepository)
{
    public async Task<Result<PagedResult<GoalTask>>> ExecuteAsync(
        Guid goalId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);

        var goalExists = await goalRepository.ExistsAsync(goalId, cancellationToken);
        if (!goalExists)
        {
            return Result<PagedResult<GoalTask>>.NotFound(UserErrorMessages.GoalNotFound);
        }

        var result = await taskRepository.GetByGoalIdAsync(goalId, page, pageSize, cancellationToken);
        return Result<PagedResult<GoalTask>>.Success(result);
    }
}
