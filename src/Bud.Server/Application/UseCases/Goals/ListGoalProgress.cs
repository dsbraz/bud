using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Application.Ports;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.UseCases.Goals;

public sealed class ListGoalProgress(IGoalProgressService goalProgressService)
{
    public async Task<Result<List<GoalProgressResponse>>> ExecuteAsync(
        List<Guid> goalIds,
        CancellationToken cancellationToken = default)
    {
        var result = await goalProgressService.GetProgressAsync(goalIds, cancellationToken);
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
