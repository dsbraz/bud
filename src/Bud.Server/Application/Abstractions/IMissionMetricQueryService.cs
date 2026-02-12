using Bud.Shared.Contracts;
using Bud.Shared.Models;

namespace Bud.Server.Application.Abstractions;

public interface IMissionMetricQueryService
{
    Task<ServiceResult<MissionMetric>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ServiceResult<PagedResult<MissionMetric>>> GetAllAsync(Guid? missionId, string? search, int page, int pageSize, CancellationToken cancellationToken = default);
}
