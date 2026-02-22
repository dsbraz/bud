using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.UseCases.Metrics;

public sealed class ListMetricCheckins(IMetricRepository metricRepository)
{
    public async Task<Result<PagedResult<MetricCheckin>>> ExecuteAsync(
        Guid metricId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var result = await metricRepository.GetCheckinsAsync(metricId, null, page, pageSize, cancellationToken);
        return Result<PagedResult<MetricCheckin>>.Success(result.MapPaged(x => x));
    }
}
