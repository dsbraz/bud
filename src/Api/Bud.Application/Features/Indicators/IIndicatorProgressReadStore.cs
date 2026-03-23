using Bud.Application.Common;

namespace Bud.Application.Features.Indicators;

public interface IIndicatorProgressReadStore
{
    Task<Result<IndicatorProgressSnapshot?>> GetIndicatorProgressAsync(Guid indicatorId, CancellationToken ct = default);
}
