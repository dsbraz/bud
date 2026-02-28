using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;

namespace Bud.Server.Application.UseCases.Indicators;

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
