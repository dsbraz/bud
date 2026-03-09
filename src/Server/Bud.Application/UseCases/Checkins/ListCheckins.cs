using Bud.Application.Common;
using Bud.Application.Mapping;
using Bud.Domain.Model;
using Bud.Domain.Repositories;

namespace Bud.Application.UseCases.Checkins;

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
