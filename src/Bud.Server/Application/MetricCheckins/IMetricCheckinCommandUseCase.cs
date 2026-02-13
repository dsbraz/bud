using System.Security.Claims;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Application.MetricCheckins;

public interface IMetricCheckinCommandUseCase
{
    Task<ServiceResult<MetricCheckin>> CreateAsync(
        ClaimsPrincipal user,
        CreateMetricCheckinRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<MetricCheckin>> UpdateAsync(
        ClaimsPrincipal user,
        Guid id,
        UpdateMetricCheckinRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult> DeleteAsync(
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken = default);
}
