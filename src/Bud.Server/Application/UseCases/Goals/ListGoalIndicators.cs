using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;

namespace Bud.Server.Application.UseCases.Goals;

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
