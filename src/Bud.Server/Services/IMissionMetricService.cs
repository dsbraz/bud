using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Services;

public interface IMissionMetricService
{
    Task<ServiceResult<MissionMetric>> CreateAsync(CreateMissionMetricRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<MissionMetric>> UpdateAsync(Guid id, UpdateMissionMetricRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ServiceResult<MissionMetric>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ServiceResult<PagedResult<MissionMetric>>> GetAllAsync(Guid? missionId, string? search, int page, int pageSize, CancellationToken cancellationToken = default);
}
