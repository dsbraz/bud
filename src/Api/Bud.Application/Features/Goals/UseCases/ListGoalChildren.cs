using Bud.Application.Common;

namespace Bud.Application.Features.Goals.UseCases;

public sealed class ListGoalChildren(IGoalRepository goalRepository)
{
    public async Task<Result<PagedResult<Goal>>> ExecuteAsync(
        Guid parentId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);

        var parentExists = await goalRepository.ExistsAsync(parentId, cancellationToken);
        if (!parentExists)
        {
            return Result<PagedResult<Goal>>.NotFound(UserErrorMessages.GoalNotFound);
        }

        var result = await goalRepository.GetChildrenAsync(parentId, page, pageSize, cancellationToken);
        return Result<PagedResult<Goal>>.Success(result.MapPaged(x => x));
    }
}
