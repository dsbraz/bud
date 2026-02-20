using Bud.Server.Infrastructure.Repositories;
using Bud.Server.Infrastructure.Services;
using Bud.Server.Application.Common;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Application.MissionObjectives;

public sealed class MissionObjectiveQueryUseCase(
    IMissionObjectiveRepository objectiveRepository,
    IMissionProgressService missionProgressService) : IMissionObjectiveQueryUseCase
{
    public async Task<Result<MissionObjective>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var objective = await objectiveRepository.GetByIdAsync(id, cancellationToken);

        return objective is null
            ? Result<MissionObjective>.NotFound("Objetivo não encontrado.")
            : Result<MissionObjective>.Success(objective);
    }

    public async Task<Result<PagedResult<MissionObjective>>> GetByMissionAsync(
        Guid missionId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var result = await objectiveRepository.GetByMissionAsync(missionId, page, pageSize, cancellationToken);
        return Result<PagedResult<MissionObjective>>.Success(result);
    }

    public async Task<Result<List<ObjectiveProgressDto>>> GetProgressAsync(
        List<Guid> objectiveIds,
        CancellationToken cancellationToken = default)
    {
        var result = await missionProgressService.GetObjectiveProgressAsync(objectiveIds, cancellationToken);
        if (!result.IsSuccess)
        {
            return Result<List<ObjectiveProgressDto>>.Failure(result.Error ?? "Falha ao calcular progresso dos objetivos.", result.ErrorType);
        }

        return Result<List<ObjectiveProgressDto>>.Success(
            result.Value!.Select(p => p.ToContract()).ToList());
    }
}
