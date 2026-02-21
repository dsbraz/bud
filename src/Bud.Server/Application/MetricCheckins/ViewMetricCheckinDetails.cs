using Bud.Server.Application.Common;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;

namespace Bud.Server.Application.MetricCheckins;

public sealed class ViewMetricCheckinDetails(IMetricCheckinRepository checkinRepository)
{
    public async Task<Result<MetricCheckin>> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var checkin = await checkinRepository.GetByIdAsync(id, cancellationToken);
        return checkin is null
            ? Result<MetricCheckin>.NotFound("Check-in não encontrado.")
            : Result<MetricCheckin>.Success(checkin);
    }
}
