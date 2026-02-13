using System.Security.Claims;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Application.Teams;

public interface ITeamCommandUseCase
{
    Task<ServiceResult<Team>> CreateAsync(
        ClaimsPrincipal user,
        CreateTeamRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<Team>> UpdateAsync(
        ClaimsPrincipal user,
        Guid id,
        UpdateTeamRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult> DeleteAsync(
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ServiceResult> UpdateCollaboratorsAsync(
        ClaimsPrincipal user,
        Guid id,
        UpdateTeamCollaboratorsRequest request,
        CancellationToken cancellationToken = default);
}
