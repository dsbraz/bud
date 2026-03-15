using Bud.Application.Common;

namespace Bud.Application.Features.Goals;

public sealed class ListGoals(IGoalRepository goalRepository)
{
    public async Task<Result<PagedResult<Goal>>> ExecuteAsync(
        GoalFilter? filter,
        Guid? collaboratorId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);

        var result = await goalRepository.GetAllAsync(
            filter,
            collaboratorId,
            search,
            page,
            pageSize,
            cancellationToken);

        return Result<PagedResult<Goal>>.Success(result.MapPaged(x => x));
    }
}
