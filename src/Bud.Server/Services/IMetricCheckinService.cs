using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Services;

public interface IMetricCheckinService
{
    Task<ServiceResult<MetricCheckin>> CreateAsync(CreateMetricCheckinRequest request, Guid collaboratorId, CancellationToken cancellationToken = default);
    Task<ServiceResult<MetricCheckin>> UpdateAsync(Guid id, UpdateMetricCheckinRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ServiceResult<MetricCheckin>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ServiceResult<PagedResult<MetricCheckin>>> GetAllAsync(Guid? missionMetricId, Guid? missionId, int page, int pageSize, CancellationToken cancellationToken = default);
}
