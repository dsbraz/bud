using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.Projections;
using Bud.Server.Authorization;
using Bud.Server.Domain.Abstractions;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Server.Domain.ValueObjects;
using Bud.Server.MultiTenancy;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.Collaborators;

public sealed class CreateCollaborator(
    ICollaboratorRepository collaboratorRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    ITenantProvider tenantProvider,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<Collaborator>> ExecuteAsync(
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
            var requestedRole = request.Role.ToDomain();

            var collaborator = Collaborator.Create(
                Guid.NewGuid(),
                organizationId.Value,
                personName.Value,
                emailAddress.Value,
                requestedRole,
                request.LeaderId);

            await collaboratorRepository.AddAsync(collaborator, cancellationToken);
            await unitOfWork.CommitAsync(collaboratorRepository.SaveChangesAsync, cancellationToken);

            return Result<Collaborator>.Success(collaborator);
        }
        catch (DomainInvariantException ex)
        {
            return Result<Collaborator>.Failure(ex.Message, ErrorType.Validation);
        }
    }
}

public sealed class UpdateCollaboratorProfile(
    ICollaboratorRepository collaboratorRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<Collaborator>> ExecuteAsync(
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

            if (leader.Role != Bud.Server.Domain.Model.CollaboratorRole.Leader)
            {
                return Result<Collaborator>.Failure("O colaborador selecionado não é um líder.", ErrorType.Validation);
            }
        }

        var requestedRole = request.Role.ToDomain();

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
                request.LeaderId,
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

public sealed class UpdateCollaboratorTeams(
    ICollaboratorRepository collaboratorRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result> ExecuteAsync(
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
                distinctTeamIds,
                collaborator.OrganizationId,
                cancellationToken);

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

        await unitOfWork.CommitAsync(collaboratorRepository.SaveChangesAsync, cancellationToken);
        return Result.Success();
    }
}

public sealed class ViewCollaboratorProfile(ICollaboratorRepository collaboratorRepository)
{
    public async Task<Result<Collaborator>> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var collaborator = await collaboratorRepository.GetByIdAsync(id, cancellationToken);
        return collaborator is null
            ? Result<Collaborator>.NotFound("Colaborador não encontrado.")
            : Result<Collaborator>.Success(collaborator);
    }
}

public sealed class ListLeaders(ICollaboratorRepository collaboratorRepository)
{
    public async Task<Result<List<LeaderCollaboratorResponse>>> ExecuteAsync(
        Guid? organizationId,
        CancellationToken cancellationToken = default)
    {
        var leaders = await collaboratorRepository.GetLeadersAsync(organizationId, cancellationToken);
        return Result<List<LeaderCollaboratorResponse>>.Success(leaders.Select(c => c.ToContract()).ToList());
    }
}

public sealed class ListCollaborators(ICollaboratorRepository collaboratorRepository)
{
    public async Task<Result<PagedResult<Collaborator>>> ExecuteAsync(
        Guid? teamId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);
        var result = await collaboratorRepository.GetAllAsync(teamId, search, page, pageSize, cancellationToken);
        return Result<PagedResult<Collaborator>>.Success(result);
    }
}

public sealed class GetCollaboratorHierarchy(ICollaboratorRepository collaboratorRepository)
{
    public async Task<Result<List<CollaboratorHierarchyNodeDto>>> ExecuteAsync(
        Guid collaboratorId,
        CancellationToken cancellationToken = default)
    {
        if (!await collaboratorRepository.ExistsAsync(collaboratorId, cancellationToken))
        {
            return Result<List<CollaboratorHierarchyNodeDto>>.NotFound("Colaborador não encontrado.");
        }

        var nodes = await collaboratorRepository.GetSubordinatesAsync(collaboratorId, 5, cancellationToken);
        return Result<List<CollaboratorHierarchyNodeDto>>.Success(nodes.Select(c => c.ToContract()).ToList());
    }
}

public sealed class ListCollaboratorTeams(ICollaboratorRepository collaboratorRepository)
{
    public async Task<Result<List<TeamSummaryDto>>> ExecuteAsync(
        Guid collaboratorId,
        CancellationToken cancellationToken = default)
    {
        if (!await collaboratorRepository.ExistsAsync(collaboratorId, cancellationToken))
        {
            return Result<List<TeamSummaryDto>>.NotFound("Colaborador não encontrado.");
        }

        var teams = await collaboratorRepository.GetTeamsAsync(collaboratorId, cancellationToken);
        return Result<List<TeamSummaryDto>>.Success(teams.Select(t => t.ToContract()).ToList());
    }
}

public sealed class ListAvailableCollaboratorTeams(ICollaboratorRepository collaboratorRepository)
{
    public async Task<Result<List<TeamSummaryDto>>> ExecuteAsync(
        Guid collaboratorId,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var collaborator = await collaboratorRepository.GetByIdAsync(collaboratorId, cancellationToken);
        if (collaborator is null)
        {
            return Result<List<TeamSummaryDto>>.NotFound("Colaborador não encontrado.");
        }

        var teams = await collaboratorRepository.GetAvailableTeamsAsync(
            collaboratorId,
            collaborator.OrganizationId,
            search,
            50,
            cancellationToken);
        return Result<List<TeamSummaryDto>>.Success(teams.Select(t => t.ToContract()).ToList());
    }
}

public sealed class ListCollaboratorSummaries(ICollaboratorRepository collaboratorRepository)
{
    public async Task<Result<List<CollaboratorSummaryDto>>> ExecuteAsync(
        string? search,
        CancellationToken cancellationToken = default)
    {
        var summaries = await collaboratorRepository.GetSummariesAsync(search, 50, cancellationToken);
        return Result<List<CollaboratorSummaryDto>>.Success(summaries.Select(c => c.ToContract()).ToList());
    }
}
