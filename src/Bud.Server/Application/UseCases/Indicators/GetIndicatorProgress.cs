using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Application.Ports;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.UseCases.Indicators;

public sealed class GetIndicatorProgress(IGoalProgressService goalProgressService)
{
    public async Task<Result<IndicatorProgressResponse?>> ExecuteAsync(
        Guid indicatorId,
        CancellationToken cancellationToken = default)
    {
        var result = await goalProgressService.GetIndicatorProgressAsync(indicatorId, cancellationToken);
        if (!result.IsSuccess)
        {
            return Result<IndicatorProgressResponse?>.Failure(
                result.Error ?? UserErrorMessages.IndicatorProgressCalculationFailed,
                result.ErrorType);
        }

        return Result<IndicatorProgressResponse?>.Success(result.Value?.ToResponse());
    }
}
