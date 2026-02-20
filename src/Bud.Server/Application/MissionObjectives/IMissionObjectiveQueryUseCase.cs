using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using Bud.Server.Application.Common;

namespace Bud.Server.Application.MissionObjectives;

public interface IMissionObjectiveQueryUseCase
{
    Task<Result<MissionObjective>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<PagedResult<MissionObjective>>> GetByMissionAsync(
        Guid missionId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Result<List<ObjectiveProgressDto>>> GetProgressAsync(
        List<Guid> objectiveIds,
        CancellationToken cancellationToken = default);
}
