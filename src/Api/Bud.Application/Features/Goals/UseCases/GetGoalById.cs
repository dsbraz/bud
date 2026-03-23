using Bud.Application.Common;

namespace Bud.Application.Features.Goals.UseCases;

public sealed class GetGoalById(IGoalRepository goalRepository)
{
    public async Task<Result<Goal>> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var goal = await goalRepository.GetByIdReadOnlyAsync(id, cancellationToken);
        return goal is null
            ? Result<Goal>.NotFound(UserErrorMessages.GoalNotFound)
            : Result<Goal>.Success(goal);
    }
}
