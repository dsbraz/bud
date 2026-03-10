using Bud.Application.Common;

namespace Bud.Application.Indicators;

public sealed class GetIndicatorById(IIndicatorRepository indicatorRepository)
{
    public async Task<Result<Indicator>> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var indicator = await indicatorRepository.GetByIdAsync(id, cancellationToken);
        return indicator is null
            ? Result<Indicator>.NotFound(UserErrorMessages.IndicatorNotFound)
            : Result<Indicator>.Success(indicator);
    }
}
