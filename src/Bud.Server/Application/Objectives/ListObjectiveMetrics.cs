using Bud.Server.Application.Common;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.Objectives;

public sealed class ListObjectiveMetrics(IMetricRepository metricRepository)
{
    public async Task<Result<PagedResult<Metric>>> ExecuteAsync(
        Guid objectiveId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var result = await metricRepository.GetAllAsync(
            missionId: null,
            objectiveId: objectiveId,
            search: null,
            page,
            pageSize,
            cancellationToken);

        return Result<PagedResult<Metric>>.Success(result.MapPaged(x => x));
    }
}
