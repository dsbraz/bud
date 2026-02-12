using Bud.Shared.Contracts;
using Bud.Shared.Models;

namespace Bud.Server.Application.Abstractions;

public interface IMetricCheckinCommandService
{
    Task<ServiceResult<MetricCheckin>> CreateAsync(CreateMetricCheckinRequest request, Guid collaboratorId, CancellationToken cancellationToken = default);
    Task<ServiceResult<MetricCheckin>> UpdateAsync(Guid id, UpdateMetricCheckinRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
