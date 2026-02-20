using System.Security.Claims;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using Bud.Server.Application.Common;

namespace Bud.Server.Application.MissionMetrics;

public interface IMissionMetricCommandUseCase
{
    Task<Result<MissionMetric>> CreateAsync(
        ClaimsPrincipal user,
        CreateMissionMetricRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<MissionMetric>> UpdateAsync(
        ClaimsPrincipal user,
        Guid id,
        UpdateMissionMetricRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken = default);
}
