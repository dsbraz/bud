using Bud.Application.Common;
using Bud.Shared.Contracts;

namespace Bud.Application.Features.Indicators;

public sealed class GetIndicatorProgress(IIndicatorProgressReadStore indicatorProgressReadStore)
{
    public async Task<Result<IndicatorProgressResponse?>> ExecuteAsync(
        Guid indicatorId,
        CancellationToken cancellationToken = default)
    {
        var result = await indicatorProgressReadStore.GetIndicatorProgressAsync(indicatorId, cancellationToken);
        if (!result.IsSuccess)
        {
            return Result<IndicatorProgressResponse?>.Failure(
                result.Error ?? UserErrorMessages.IndicatorProgressCalculationFailed,
                result.ErrorType);
        }

        return Result<IndicatorProgressResponse?>.Success(result.Value?.ToResponse());
    }
}
