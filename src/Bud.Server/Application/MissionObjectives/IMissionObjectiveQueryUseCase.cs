using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Application.MissionObjectives;

public interface IMissionObjectiveQueryUseCase
{
    Task<ServiceResult<MissionObjective>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ServiceResult<PagedResult<MissionObjective>>> GetByMissionAsync(
        Guid missionId,
        Guid? parentObjectiveId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<List<ObjectiveProgressDto>>> GetProgressAsync(
        List<Guid> objectiveIds,
        CancellationToken cancellationToken = default);
}
