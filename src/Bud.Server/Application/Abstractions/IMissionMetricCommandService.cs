using Bud.Shared.Contracts;
using Bud.Shared.Models;

namespace Bud.Server.Application.Abstractions;

public interface IMissionMetricCommandService
{
    Task<ServiceResult<MissionMetric>> CreateAsync(CreateMissionMetricRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<MissionMetric>> UpdateAsync(Guid id, UpdateMissionMetricRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
