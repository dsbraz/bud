using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;

namespace Bud.Server.Application.UseCases.Checkins;

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
            return Result<Checkin>.NotFound("Check-in não encontrado.");
        }

        return Result<Checkin>.Success(checkin);
    }
}
