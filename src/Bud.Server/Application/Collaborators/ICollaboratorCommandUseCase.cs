using System.Security.Claims;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Application.Collaborators;

public interface ICollaboratorCommandUseCase
{
    Task<ServiceResult<Collaborator>> CreateAsync(
        ClaimsPrincipal user,
        CreateCollaboratorRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<Collaborator>> UpdateAsync(
        ClaimsPrincipal user,
        Guid id,
        UpdateCollaboratorRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult> DeleteAsync(
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ServiceResult> UpdateTeamsAsync(
        ClaimsPrincipal user,
        Guid id,
        UpdateCollaboratorTeamsRequest request,
        CancellationToken cancellationToken = default);
}
