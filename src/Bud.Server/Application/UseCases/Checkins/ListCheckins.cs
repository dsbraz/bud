using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;

namespace Bud.Server.Application.UseCases.Checkins;

public sealed class ListCheckins(IIndicatorRepository indicatorRepository)
{
    public async Task<Result<PagedResult<Checkin>>> ExecuteAsync(
        Guid indicatorId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);
        var result = await indicatorRepository.GetCheckinsAsync(indicatorId, null, page, pageSize, cancellationToken);
        return Result<PagedResult<Checkin>>.Success(result.MapPaged(x => x));
    }
}
