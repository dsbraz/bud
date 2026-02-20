using System.Security.Claims;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using Bud.Server.Application.Common;

namespace Bud.Server.Application.MissionObjectives;

public interface IMissionObjectiveCommandUseCase
{
    Task<Result<MissionObjective>> CreateAsync(
        ClaimsPrincipal user,
        CreateMissionObjectiveRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<MissionObjective>> UpdateAsync(
        ClaimsPrincipal user,
        Guid id,
        UpdateMissionObjectiveRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken = default);
}
