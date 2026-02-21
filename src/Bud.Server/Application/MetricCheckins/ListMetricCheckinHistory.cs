using Bud.Server.Application.Common;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.MetricCheckins;

public sealed class ListMetricCheckinHistory(IMetricCheckinRepository checkinRepository)
{
    public async Task<Result<PagedResult<MetricCheckin>>> ExecuteAsync(
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
