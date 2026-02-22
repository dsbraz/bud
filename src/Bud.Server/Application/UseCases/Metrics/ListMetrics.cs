using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.UseCases.Metrics;

public sealed class ListMetrics(IMetricRepository metricRepository)
{
    public async Task<Result<Bud.Shared.Contracts.Common.PagedResult<Metric>>> ExecuteAsync(
        Guid? missionId,
        Guid? objectiveId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);
        var result = await metricRepository.GetAllAsync(missionId, objectiveId, search, page, pageSize, cancellationToken);
        return Result<Bud.Shared.Contracts.Common.PagedResult<Metric>>.Success(result.MapPaged(x => x));
    }
}

