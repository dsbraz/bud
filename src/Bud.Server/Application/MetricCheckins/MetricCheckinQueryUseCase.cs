using Bud.Server.Services;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Application.MetricCheckins;

public sealed class MetricCheckinQueryUseCase(
    IMetricCheckinService checkinService) : IMetricCheckinQueryUseCase
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
