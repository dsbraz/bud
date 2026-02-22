using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;

namespace Bud.Server.Application.UseCases.Objectives;

public sealed class GetObjectiveById(IObjectiveRepository objectiveRepository)
{
    public async Task<Result<Objective>> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var objective = await objectiveRepository.GetByIdAsync(id, cancellationToken);

        return objective is null
            ? Result<Objective>.NotFound("Objetivo não encontrado.")
            : Result<Objective>.Success(objective);
    }
}
