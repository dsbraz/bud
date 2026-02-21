using Bud.Server.Infrastructure.Repositories;
using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Authorization;
using Bud.Server.Domain.ValueObjects;
using Bud.Server.MultiTenancy;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Application.Collaborators;

public sealed class CollaboratorCommand(
    ICollaboratorRepository collaboratorRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    ITenantProvider tenantProvider)
{
    public async Task<Result<Collaborator>> CreateAsync(
        ClaimsPrincipal user,
        CreateCollaboratorRequest request,
        CancellationToken cancellationToken = default)
    {
        var organizationId = tenantProvider.TenantId;
        if (!organizationId.HasValue)
        {
            return Result<Collaborator>.Failure("Contexto de organização não encontrado.", ErrorType.Validation);
        }

        var canCreate = await authorizationGateway.IsOrganizationOwnerAsync(user, organizationId.Value, cancellationToken);
        if (!canCreate)
        {
            return Result<Collaborator>.Forbidden("Apenas o proprietário da organização pode criar colaboradores.");
        }

        if (!EmailAddress.TryCreate(request.Email, out var emailAddress))
        {
            return Result<Collaborator>.Failure("E-mail inválido.", ErrorType.Validation);
        }

        if (!PersonName.TryCreate(request.FullName, out var personName))
        {
            return Result<Collaborator>.Failure("O nome do colaborador é obrigatório.", ErrorType.Validation);
        }

        try
        {
            var collaborator = Collaborator.Create(
                Guid.NewGuid(),
                organizationId.Value,
                personName.Value,
                emailAddress.Value,
                request.Role,
                request.LeaderId);

            await collaboratorRepository.AddAsync(collaborator, cancellationToken);
            await collaboratorRepository.SaveChangesAsync(cancellationToken);

            return Result<Collaborator>.Success(collaborator);
        }
        catch (DomainInvariantException ex)
        {
            return Result<Collaborator>.Failure(ex.Message, ErrorType.Validation);
        }
    }

    public async Task<Result<Collaborator>> UpdateAsync(
        ClaimsPrincipal user,
        Guid id,
        UpdateCollaboratorRequest request,
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

        if (!EmailAddress.TryCreate(request.Email, out var emailAddress))
        {
            return Result<Collaborator>.Failure("E-mail inválido.", ErrorType.Validation);
        }

        if (!PersonName.TryCreate(request.FullName, out var personName))
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

        if (request.LeaderId.HasValue)
        {
            var leader = await collaboratorRepository.GetByIdAsync(request.LeaderId.Value, cancellationToken);
            if (leader is null)
            {
                return Result<Collaborator>.NotFound("Líder não encontrado.");
            }

            if (leader.OrganizationId != collaborator.OrganizationId)
            {
                return Result<Collaborator>.Failure("O líder deve pertencer à mesma organização.", ErrorType.Validation);
            }

            if (leader.Role != CollaboratorRole.Leader)
            {
                return Result<Collaborator>.Failure("O colaborador selecionado não é um líder.", ErrorType.Validation);
            }
        }

        if (collaborator.Role == CollaboratorRole.Leader &&
            request.Role == CollaboratorRole.IndividualContributor)
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
                request.Role,
                request.LeaderId,
                collaborator.Id);
            await collaboratorRepository.SaveChangesAsync(cancellationToken);

            return Result<Collaborator>.Success(collaborator);
        }
        catch (DomainInvariantException ex)
        {
            return Result<Collaborator>.Failure(ex.Message, ErrorType.Validation);
        }
    }

    public async Task<Result> DeleteAsync(
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
        await collaboratorRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> UpdateTeamsAsync(
        ClaimsPrincipal user,
        Guid id,
        UpdateCollaboratorTeamsRequest request,
        CancellationToken cancellationToken = default)
    {
        var collaborator = await collaboratorRepository.GetByIdWithCollaboratorTeamsAsync(id, cancellationToken);
        if (collaborator is null)
        {
            return Result.NotFound("Colaborador não encontrado.");
        }

        var canAssign = await authorizationGateway.IsOrganizationOwnerAsync(user, collaborator.OrganizationId, cancellationToken);
        if (!canAssign)
        {
            return Result.Forbidden("Apenas o proprietário da organização pode atribuir equipes.");
        }

        var distinctTeamIds = request.TeamIds.Distinct().ToList();

        if (distinctTeamIds.Count > 0)
        {
            var validCount = await collaboratorRepository.CountTeamsByIdsAndOrganizationAsync(
                distinctTeamIds, collaborator.OrganizationId, cancellationToken);

            if (validCount != distinctTeamIds.Count)
            {
                return Result.Failure("Uma ou mais equipes são inválidas ou pertencem a outra organização.", ErrorType.Validation);
            }
        }

        collaborator.CollaboratorTeams.Clear();

        foreach (var teamId in distinctTeamIds)
        {
            collaborator.CollaboratorTeams.Add(new CollaboratorTeam
            {
                CollaboratorId = id,
                TeamId = teamId,
                AssignedAt = DateTime.UtcNow
            });
        }

        await collaboratorRepository.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
