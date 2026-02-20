using System.Security.Claims;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using Bud.Server.Application.Common;

namespace Bud.Server.Application.Collaborators;

public interface ICollaboratorCommandUseCase
{
    Task<Result<Collaborator>> CreateAsync(
        ClaimsPrincipal user,
        CreateCollaboratorRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<Collaborator>> UpdateAsync(
        ClaimsPrincipal user,
        Guid id,
        UpdateCollaboratorRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Result> UpdateTeamsAsync(
        ClaimsPrincipal user,
        Guid id,
        UpdateCollaboratorTeamsRequest request,
        CancellationToken cancellationToken = default);
}
