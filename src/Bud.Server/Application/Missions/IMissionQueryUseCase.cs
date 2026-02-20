using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using Bud.Server.Application.Common;

namespace Bud.Server.Application.Missions;

public interface IMissionQueryUseCase
{
    Task<Result<Mission>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<PagedResult<Mission>>> GetAllAsync(
        MissionScopeType? scopeType,
        Guid? scopeId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Result<PagedResult<Mission>>> GetMyMissionsAsync(
        Guid collaboratorId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Result<List<MissionProgressDto>>> GetProgressAsync(
        List<Guid> missionIds,
        CancellationToken cancellationToken = default);

    Task<Result<PagedResult<MissionMetric>>> GetMetricsAsync(
        Guid id,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
