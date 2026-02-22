using Bud.Server.Application.Common;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.Missions;

public sealed class ListMissionMetrics(IMissionRepository missionRepository)
{
    public async Task<Result<Bud.Shared.Contracts.PagedResult<Metric>>> ExecuteAsync(
        Guid missionId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);

        var missionExists = await missionRepository.ExistsAsync(missionId, cancellationToken);
        if (!missionExists)
        {
            return Result<Bud.Shared.Contracts.PagedResult<Metric>>.NotFound("Missão não encontrada.");
        }

        var result = await missionRepository.GetMetricsAsync(missionId, page, pageSize, cancellationToken);
        return Result<Bud.Shared.Contracts.PagedResult<Metric>>.Success(result.MapPaged(x => x));
    }
}
