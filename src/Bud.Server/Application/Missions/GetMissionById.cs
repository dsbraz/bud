using Bud.Server.Application.Common;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;

namespace Bud.Server.Application.Missions;

public sealed class GetMissionById(IMissionRepository missionRepository)
{
    public async Task<Result<Mission>> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var mission = await missionRepository.GetByIdReadOnlyAsync(id, cancellationToken);
        return mission is null
            ? Result<Mission>.NotFound("Missão não encontrada.")
            : Result<Mission>.Success(mission);
    }
}
