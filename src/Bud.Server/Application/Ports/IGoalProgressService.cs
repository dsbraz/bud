using Bud.Server.Application.ReadModels;
using Bud.Server.Application.Common;

namespace Bud.Server.Application.Ports;

public interface IGoalProgressService
{
    Task<Result<List<GoalProgressSnapshot>>> GetProgressAsync(List<Guid> goalIds, CancellationToken ct = default);
    Task<Result<IndicatorProgressSnapshot?>> GetIndicatorProgressAsync(Guid indicatorId, CancellationToken ct = default);
}
