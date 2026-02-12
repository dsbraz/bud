using Bud.Server.Application.Abstractions;
using Bud.Shared.Contracts;
using Bud.Shared.Models;

namespace Bud.Server.Application.MetricCheckins;

public sealed class MetricCheckinQueryUseCase(
    IMetricCheckinQueryService checkinService) : IMetricCheckinQueryUseCase
{
    public Task<ServiceResult<MetricCheckin>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => checkinService.GetByIdAsync(id, cancellationToken);

    public Task<ServiceResult<PagedResult<MetricCheckin>>> GetAllAsync(
        Guid? missionMetricId,
        Guid? missionId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
        => checkinService.GetAllAsync(missionMetricId, missionId, page, pageSize, cancellationToken);
}
