using Bud.Application.Common;
using Bud.Application.Mapping;
using Bud.Domain.Model;
using Bud.Domain.Repositories;

namespace Bud.Application.UseCases.Indicators;

public sealed class ListIndicators(IIndicatorRepository indicatorRepository)
{
    public async Task<Result<PagedResult<Indicator>>> ExecuteAsync(
        Guid? goalId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);
        var result = await indicatorRepository.GetAllAsync(goalId, search, page, pageSize, cancellationToken);
        return Result<PagedResult<Indicator>>.Success(result.MapPaged(x => x));
    }
}
