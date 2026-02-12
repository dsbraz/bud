using Bud.Shared.Contracts;
using Bud.Shared.Models;

namespace Bud.Server.Application.Abstractions;

public interface IMissionQueryService
{
    Task<ServiceResult<Mission>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ServiceResult<PagedResult<Mission>>> GetAllAsync(
        MissionScopeType? scopeType,
        Guid? scopeId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<ServiceResult<PagedResult<Mission>>> GetMyMissionsAsync(
        Guid collaboratorId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<ServiceResult<PagedResult<MissionMetric>>> GetMetricsAsync(Guid id, int page, int pageSize, CancellationToken cancellationToken = default);
}
