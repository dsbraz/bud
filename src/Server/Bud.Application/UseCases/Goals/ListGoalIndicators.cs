using Bud.Application.Common;
using Bud.Application.Mapping;
using Bud.Domain.Model;
using Bud.Domain.Repositories;

namespace Bud.Application.UseCases.Goals;

public sealed class ListGoalIndicators(IGoalRepository goalRepository)
{
    public async Task<Result<PagedResult<Indicator>>> ExecuteAsync(
        Guid goalId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);

        var goalExists = await goalRepository.ExistsAsync(goalId, cancellationToken);
        if (!goalExists)
        {
            return Result<PagedResult<Indicator>>.NotFound(UserErrorMessages.GoalNotFound);
        }

        var result = await goalRepository.GetIndicatorsAsync(goalId, page, pageSize, cancellationToken);
        return Result<PagedResult<Indicator>>.Success(result.MapPaged(x => x));
    }
}
