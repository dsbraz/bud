using Bud.Application.Common;

namespace Bud.Application.Goals;

public interface IGoalProgressReadStore
{
    Task<Result<List<GoalProgressSnapshot>>> GetProgressAsync(List<Guid> goalIds, CancellationToken ct = default);
}
