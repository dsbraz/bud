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

public sealed class PatchCollaborator(
    ICollaboratorRepository collaboratorRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<Collaborator>> ExecuteAsync(
        ClaimsPrincipal user,
        Guid id,
        PatchCollaboratorRequest request,
        CancellationToken cancellationToken = default)
    {
        var collaborator = await collaboratorRepository.GetByIdAsync(id, cancellationToken);
        if (collaborator is null)
        {
            return Result<Collaborator>.NotFound("Colaborador não encontrado.");
        }

        var canUpdate = await authorizationGateway.IsOrganizationOwnerAsync(user, collaborator.OrganizationId, cancellationToken);
        if (!canUpdate)
        {
            return Result<Collaborator>.Forbidden("Apenas o proprietário da organização pode editar colaboradores.");
        }

        var requestedEmail = request.Email.HasValue ? request.Email.Value : collaborator.Email;
        var requestedFullName = request.FullName.HasValue ? request.FullName.Value : collaborator.FullName;
        var requestedLeaderId = request.LeaderId.HasValue ? request.LeaderId.Value : collaborator.LeaderId;
        var requestedRole = request.Role.HasValue ? request.Role.Value.ToDomain() : collaborator.Role;

        if (!EmailAddress.TryCreate(requestedEmail, out var emailAddress))
        {
            return Result<Collaborator>.Failure("E-mail inválido.", ErrorType.Validation);
        }

        if (!PersonName.TryCreate(requestedFullName, out var personName))
        {
            return Result<Collaborator>.Failure("O nome do colaborador é obrigatório.", ErrorType.Validation);
        }

        if (collaborator.Email != emailAddress.Value)
        {
            if (!await collaboratorRepository.IsEmailUniqueAsync(emailAddress.Value, id, cancellationToken))
            {
                return Result<Collaborator>.Failure("O email já está em uso.", ErrorType.Validation);
            }
        }

        if (request.LeaderId.HasValue && requestedLeaderId.HasValue)
        {
            var leader = await collaboratorRepository.GetByIdAsync(requestedLeaderId.Value, cancellationToken);
            if (leader is null)
            {
                return Result<Collaborator>.NotFound("Líder não encontrado.");
            }

            if (leader.OrganizationId != collaborator.OrganizationId)
            {
                return Result<Collaborator>.Failure("O líder deve pertencer à mesma organização.", ErrorType.Validation);
            }

            if (leader.Role != Bud.Server.Domain.Model.CollaboratorRole.Leader)
            {
                return Result<Collaborator>.Failure("O colaborador selecionado não é um líder.", ErrorType.Validation);
            }
        }

        if (collaborator.Role == Bud.Server.Domain.Model.CollaboratorRole.Leader &&
            requestedRole == Bud.Server.Domain.Model.CollaboratorRole.IndividualContributor)
        {
            if (await collaboratorRepository.HasSubordinatesAsync(id, cancellationToken))
            {
                return Result<Collaborator>.Failure(
                    "Não é possível alterar o perfil. Este líder possui membros de equipe.",
                    ErrorType.Validation);
            }

            if (await collaboratorRepository.IsOrganizationOwnerAsync(id, cancellationToken))
            {
                return Result<Collaborator>.Failure(
                    "Não é possível alterar o perfil. Este líder é proprietário de uma organização.",
                    ErrorType.Validation);
            }
        }

        try
        {
            collaborator.UpdateProfile(
                personName.Value,
                emailAddress.Value,
                requestedRole,
                requestedLeaderId,
                collaborator.Id);
            await unitOfWork.CommitAsync(collaboratorRepository.SaveChangesAsync, cancellationToken);

            return Result<Collaborator>.Success(collaborator);
        }
        catch (DomainInvariantException ex)
        {
            return Result<Collaborator>.Failure(ex.Message, ErrorType.Validation);
        }
    }
}
