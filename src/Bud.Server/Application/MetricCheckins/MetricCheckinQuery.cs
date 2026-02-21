using Bud.Server.Infrastructure.Repositories;
using Bud.Server.Application.Common;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Application.MetricCheckins;

public sealed class MetricCheckinQuery(
    IMetricCheckinRepository checkinRepository)
{
    public async Task<Result<MetricCheckin>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var checkin = await checkinRepository.GetByIdAsync(id, cancellationToken);
        return checkin is null
            ? Result<MetricCheckin>.NotFound("Check-in não encontrado.")
            : Result<MetricCheckin>.Success(checkin);
    }

    public async Task<Result<PagedResult<MetricCheckin>>> GetAllAsync(
        Guid? missionMetricId,
        Guid? missionId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var result = await checkinRepository.GetAllAsync(missionMetricId, missionId, page, pageSize, cancellationToken);
        return Result<PagedResult<MetricCheckin>>.Success(result);
    }
}
