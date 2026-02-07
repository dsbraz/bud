using Bud.Shared.Contracts;
using Bud.Shared.Models;

namespace Bud.Server.Application.MetricCheckins;

public interface IMetricCheckinQueryUseCase
{
    Task<ServiceResult<MetricCheckin>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ServiceResult<PagedResult<MetricCheckin>>> GetAllAsync(
        Guid? missionMetricId,
        Guid? missionId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
