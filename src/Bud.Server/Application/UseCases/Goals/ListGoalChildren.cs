using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;

namespace Bud.Server.Application.UseCases.Goals;

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
            return Result<PagedResult<Goal>>.NotFound("Meta não encontrada.");
        }

        var result = await goalRepository.GetChildrenAsync(parentId, page, pageSize, cancellationToken);
        return Result<PagedResult<Goal>>.Success(result.MapPaged(x => x));
    }
}
