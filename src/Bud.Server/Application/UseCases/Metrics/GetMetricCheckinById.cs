using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;

namespace Bud.Server.Application.UseCases.Metrics;

public sealed class GetMetricCheckinById(IMetricRepository metricRepository)
{
    public async Task<Result<MetricCheckin>> ExecuteAsync(
        Guid metricId,
        Guid checkinId,
        CancellationToken cancellationToken = default)
    {
        var checkin = await metricRepository.GetCheckinByIdAsync(checkinId, cancellationToken);
        if (checkin is null || checkin.MetricId != metricId)
        {
            return Result<MetricCheckin>.NotFound("Check-in não encontrado.");
        }

        return Result<MetricCheckin>.Success(checkin);
    }
}
