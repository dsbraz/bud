using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Domain.ReadModels;
using Bud.Server.Authorization;
using Bud.Server.Domain.Abstractions;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Server.Domain.ValueObjects;
using Bud.Server.MultiTenancy;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.UseCases.Collaborators;

public sealed class DeleteCollaborator(
    ICollaboratorRepository collaboratorRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result> ExecuteAsync(
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var collaborator = await collaboratorRepository.GetByIdAsync(id, cancellationToken);
        if (collaborator is null)
        {
            return Result.NotFound("Colaborador não encontrado.");
        }

        var canDelete = await authorizationGateway.IsOrganizationOwnerAsync(user, collaborator.OrganizationId, cancellationToken);
        if (!canDelete)
        {
            return Result.Forbidden("Apenas o proprietário da organização pode excluir colaboradores.");
        }

        if (await collaboratorRepository.IsOrganizationOwnerAsync(id, cancellationToken))
        {
            return Result.Failure(
                "Não é possível excluir o colaborador. Ele é proprietário de uma organização.",
                ErrorType.Conflict);
        }

        if (await collaboratorRepository.HasSubordinatesAsync(id, cancellationToken))
        {
            return Result.Failure(
                "Não é possível excluir o colaborador. Ele é líder de outros colaboradores.",
                ErrorType.Conflict);
        }

        if (await collaboratorRepository.HasMissionsAsync(id, cancellationToken))
        {
            return Result.Failure(
                "Não é possível excluir o colaborador porque existem missões associadas a ele.",
                ErrorType.Conflict);
        }

        await collaboratorRepository.RemoveAsync(collaborator, cancellationToken);
        await unitOfWork.CommitAsync(collaboratorRepository.SaveChangesAsync, cancellationToken);

        return Result.Success();
    }
}

