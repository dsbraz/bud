using Bud.Shared.Contracts;
using Bud.Shared.Models;

namespace Bud.Server.Application.Abstractions;

public interface ICollaboratorCommandService
{
    Task<ServiceResult<Collaborator>> CreateAsync(CreateCollaboratorRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<Collaborator>> UpdateAsync(Guid id, UpdateCollaboratorRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ServiceResult> UpdateTeamsAsync(Guid collaboratorId, UpdateCollaboratorTeamsRequest request, CancellationToken cancellationToken = default);
}
