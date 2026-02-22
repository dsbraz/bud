using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.UseCases.Objectives;

public sealed class ListObjectives(IObjectiveRepository objectiveRepository)
{
    public async Task<Result<Bud.Shared.Contracts.Common.PagedResult<Objective>>> ExecuteAsync(
        Guid? missionId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var result = await objectiveRepository.GetAllAsync(missionId, page, pageSize, cancellationToken);
        return Result<Bud.Shared.Contracts.Common.PagedResult<Objective>>.Success(result.MapPaged(x => x));
    }
}
