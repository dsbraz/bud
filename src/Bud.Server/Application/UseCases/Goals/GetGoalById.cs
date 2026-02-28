using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;

namespace Bud.Server.Application.UseCases.Goals;

public sealed class GetGoalById(IGoalRepository goalRepository)
{
    public async Task<Result<Goal>> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var goal = await goalRepository.GetByIdReadOnlyAsync(id, cancellationToken);
        return goal is null
            ? Result<Goal>.NotFound("Meta não encontrada.")
            : Result<Goal>.Success(goal);
    }
}
