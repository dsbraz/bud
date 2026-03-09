using Bud.Application.ReadModels;
using Bud.Application.Common;

namespace Bud.Application.Ports;

public interface IGoalProgressService
{
    Task<Result<List<GoalProgressSnapshot>>> GetProgressAsync(List<Guid> goalIds, CancellationToken ct = default);
    Task<Result<IndicatorProgressSnapshot?>> GetIndicatorProgressAsync(Guid indicatorId, CancellationToken ct = default);
}
