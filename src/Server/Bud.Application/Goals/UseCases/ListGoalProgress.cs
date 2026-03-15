using Bud.Application.Common;
using Bud.Shared.Contracts;

namespace Bud.Application.Goals;

public sealed class ListGoalProgress(IGoalProgressReadStore goalProgressReadStore)
{
    public async Task<Result<List<GoalProgressResponse>>> ExecuteAsync(
        List<Guid> goalIds,
        CancellationToken cancellationToken = default)
    {
        var result = await goalProgressReadStore.GetProgressAsync(goalIds, cancellationToken);
        if (!result.IsSuccess)
        {
            return Result<List<GoalProgressResponse>>.Failure(
                result.Error ?? UserErrorMessages.GoalProgressCalculationFailed,
                result.ErrorType);
        }

        return Result<List<GoalProgressResponse>>.Success(
            result.Value!.Select(p => p.ToResponse()).ToList());
    }
}
