using Bud.Server.Application.Common;
using Bud.Server.Application.Projections;
using Bud.Server.Infrastructure.Services;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.Missions;

public sealed class ListMissionProgress(IMissionProgressService missionProgressService)
{
    public async Task<Result<List<MissionProgressDto>>> ExecuteAsync(
        List<Guid> missionIds,
        CancellationToken cancellationToken = default)
    {
        var result = await missionProgressService.GetProgressAsync(missionIds, cancellationToken);
        if (!result.IsSuccess)
        {
            return Result<List<MissionProgressDto>>.Failure(
                result.Error ?? "Falha ao calcular progresso das missões.",
                result.ErrorType);
        }

        return Result<List<MissionProgressDto>>.Success(
            result.Value!.Select(p => p.ToContract()).ToList());
    }
}
