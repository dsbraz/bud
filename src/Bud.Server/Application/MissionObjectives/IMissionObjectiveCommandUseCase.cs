using System.Security.Claims;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Application.MissionObjectives;

public interface IMissionObjectiveCommandUseCase
{
    Task<ServiceResult<MissionObjective>> CreateAsync(
        ClaimsPrincipal user,
        CreateMissionObjectiveRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<MissionObjective>> UpdateAsync(
        ClaimsPrincipal user,
        Guid id,
        UpdateMissionObjectiveRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult> DeleteAsync(
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken = default);
}
