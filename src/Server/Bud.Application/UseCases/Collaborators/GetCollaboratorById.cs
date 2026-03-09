using System.Security.Claims;
using Bud.Application.Common;
using Bud.Application.Mapping;
using Bud.Application.Ports;
using Bud.Domain.Model;
using Bud.Domain.Repositories;
using Bud.Shared.Contracts;

namespace Bud.Application.UseCases.Collaborators;

public sealed class GetCollaboratorById(ICollaboratorRepository collaboratorRepository)
{
    public async Task<Result<Collaborator>> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var collaborator = await collaboratorRepository.GetByIdAsync(id, cancellationToken);
        return collaborator is null
            ? Result<Collaborator>.NotFound(UserErrorMessages.CollaboratorNotFound)
            : Result<Collaborator>.Success(collaborator);
    }
}

