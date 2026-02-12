using Bud.Server.Application.Abstractions;
using Bud.Shared.Contracts;
using Bud.Shared.Models;

namespace Bud.Server.Application.Missions;

public sealed class MissionQueryUseCase(
    IMissionQueryService missionService,
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

    public Task<ServiceResult<List<MissionProgressDto>>> GetProgressAsync(
        List<Guid> missionIds,
        CancellationToken cancellationToken = default)
        => missionProgressService.GetProgressAsync(missionIds, cancellationToken);

    public Task<ServiceResult<PagedResult<MissionMetric>>> GetMetricsAsync(
        Guid id,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
        => missionService.GetMetricsAsync(id, page, pageSize, cancellationToken);
}
