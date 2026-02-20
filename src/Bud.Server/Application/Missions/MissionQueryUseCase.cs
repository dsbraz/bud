using Bud.Server.Services;
using Bud.Server.Application.Common;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Application.Missions;

public sealed class MissionQueryUseCase(
    IMissionService missionService,
    IMissionProgressService missionProgressService) : IMissionQueryUseCase
{
    public Task<ServiceResult<Mission>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => missionService.GetByIdAsync(id, cancellationToken);

    public Task<ServiceResult<PagedResult<Mission>>> GetAllAsync(
        MissionScopeType? scopeType,
        Guid? scopeId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
        => missionService.GetAllAsync(scopeType, scopeId, search, page, pageSize, cancellationToken);

    public Task<ServiceResult<PagedResult<Mission>>> GetMyMissionsAsync(
        Guid collaboratorId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
        => missionService.GetMyMissionsAsync(collaboratorId, search, page, pageSize, cancellationToken);

    public async Task<ServiceResult<List<MissionProgressDto>>> GetProgressAsync(
        List<Guid> missionIds,
        CancellationToken cancellationToken = default)
    {
        var result = await missionProgressService.GetProgressAsync(missionIds, cancellationToken);
        if (!result.IsSuccess)
        {
            return ServiceResult<List<MissionProgressDto>>.Failure(result.Error ?? "Falha ao calcular progresso das missões.", result.ErrorType);
        }

        return ServiceResult<List<MissionProgressDto>>.Success(
            result.Value!.Select(p => p.ToContract()).ToList());
    }

    public Task<ServiceResult<PagedResult<MissionMetric>>> GetMetricsAsync(
        Guid id,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
        => missionService.GetMetricsAsync(id, page, pageSize, cancellationToken);
}
