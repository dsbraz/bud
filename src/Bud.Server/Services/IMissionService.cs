using Bud.Shared.Contracts;
using Bud.Shared.Models;

namespace Bud.Server.Services;

public interface IMissionService
{
    Task<ServiceResult<Mission>> CreateAsync(CreateMissionRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<Mission>> UpdateAsync(Guid id, UpdateMissionRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
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
