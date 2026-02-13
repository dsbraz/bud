using System.Security.Claims;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Application.MissionMetrics;

public interface IMissionMetricCommandUseCase
{
    Task<ServiceResult<MissionMetric>> CreateAsync(
        ClaimsPrincipal user,
        CreateMissionMetricRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<MissionMetric>> UpdateAsync(
        ClaimsPrincipal user,
        Guid id,
        UpdateMissionMetricRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult> DeleteAsync(
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken = default);
}
