using Bud.Application.Common;

namespace Bud.Application.Indicators;

public sealed class GetCheckinById(IIndicatorRepository indicatorRepository)
{
    public async Task<Result<Checkin>> ExecuteAsync(
        Guid indicatorId,
        Guid checkinId,
        CancellationToken cancellationToken = default)
    {
        var checkin = await indicatorRepository.GetCheckinByIdAsync(checkinId, cancellationToken);
        if (checkin is null || checkin.IndicatorId != indicatorId)
        {
            return Result<Checkin>.NotFound(UserErrorMessages.CheckinNotFound);
        }

        return Result<Checkin>.Success(checkin);
    }
}
