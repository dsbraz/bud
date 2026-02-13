using System.Security.Claims;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Application.Missions;

public interface IMissionCommandUseCase
{
    Task<ServiceResult<Mission>> CreateAsync(
        ClaimsPrincipal user,
        CreateMissionRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<Mission>> UpdateAsync(
        ClaimsPrincipal user,
        Guid id,
        UpdateMissionRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult> DeleteAsync(
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken = default);
}
