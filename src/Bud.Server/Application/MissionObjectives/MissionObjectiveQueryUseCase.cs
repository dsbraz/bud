using Bud.Server.Services;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Application.MissionObjectives;

public sealed class MissionObjectiveQueryUseCase(
    IMissionObjectiveService objectiveService,
    IMissionProgressService missionProgressService) : IMissionObjectiveQueryUseCase
{
    public Task<ServiceResult<MissionObjective>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => objectiveService.GetByIdAsync(id, cancellationToken);

    public Task<ServiceResult<PagedResult<MissionObjective>>> GetByMissionAsync(
        Guid missionId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
        => objectiveService.GetByMissionAsync(missionId, page, pageSize, cancellationToken);

    public Task<ServiceResult<List<ObjectiveProgressDto>>> GetProgressAsync(
        List<Guid> objectiveIds,
        CancellationToken cancellationToken = default)
        => missionProgressService.GetObjectiveProgressAsync(objectiveIds, cancellationToken);
}
