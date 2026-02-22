using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Domain.ReadModels;
using Bud.Server.Application.Ports;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.UseCases.Objectives;

public sealed class ListObjectiveProgress(IMissionProgressService missionProgressService)
{
    public async Task<Result<List<ObjectiveProgressResponse>>> ExecuteAsync(
        List<Guid> objectiveIds,
        CancellationToken cancellationToken = default)
    {
        var result = await missionProgressService.GetObjectiveProgressAsync(objectiveIds, cancellationToken);
        if (!result.IsSuccess)
        {
            return Result<List<ObjectiveProgressResponse>>.Failure(
                result.Error ?? "Falha ao calcular progresso dos objetivos.",
                result.ErrorType);
        }

        return Result<List<ObjectiveProgressResponse>>.Success(result.Value!.Select(progress => progress.ToResponse()).ToList());
    }
}
