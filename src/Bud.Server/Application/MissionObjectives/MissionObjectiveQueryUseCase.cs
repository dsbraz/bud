using Bud.Server.Services;
using Bud.Server.Application.Common;
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

    public async Task<ServiceResult<List<ObjectiveProgressDto>>> GetProgressAsync(
        List<Guid> objectiveIds,
        CancellationToken cancellationToken = default)
    {
        var result = await missionProgressService.GetObjectiveProgressAsync(objectiveIds, cancellationToken);
        if (!result.IsSuccess)
        {
            return ServiceResult<List<ObjectiveProgressDto>>.Failure(result.Error ?? "Falha ao calcular progresso dos objetivos.", result.ErrorType);
        }

        return ServiceResult<List<ObjectiveProgressDto>>.Success(
            result.Value!.Select(p => p.ToContract()).ToList());
    }
}
