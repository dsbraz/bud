using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Domain.ReadModels;
using Bud.Server.Authorization;
using Bud.Server.Domain.Abstractions;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Server.Domain.ValueObjects;
using Bud.Server.MultiTenancy;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.Collaborators;

public sealed class GetCollaboratorById(ICollaboratorRepository collaboratorRepository)
{
    public async Task<Result<Collaborator>> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var collaborator = await collaboratorRepository.GetByIdAsync(id, cancellationToken);
        return collaborator is null
            ? Result<Collaborator>.NotFound("Colaborador não encontrado.")
            : Result<Collaborator>.Success(collaborator);
    }
}

