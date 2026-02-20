using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.Ports;
using Bud.Server.Authorization;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Application.Teams;

public sealed class TeamCommandUseCase(
    ITeamRepository teamRepository,
    IWorkspaceRepository workspaceRepository,
    ICollaboratorRepository collaboratorRepository,
    IApplicationAuthorizationGateway authorizationGateway) : ITeamCommandUseCase
{
    public async Task<Result<Team>> CreateAsync(
        ClaimsPrincipal user,
        CreateTeamRequest request,
        CancellationToken cancellationToken = default)
    {
        var workspace = await workspaceRepository.GetByIdAsync(request.WorkspaceId, cancellationToken);
        if (workspace is null)
        {
            return Result<Team>.NotFound("Workspace não encontrado.");
        }

        var canCreate = await authorizationGateway.IsOrganizationOwnerAsync(user, workspace.OrganizationId, cancellationToken);
        if (!canCreate)
        {
            return Result<Team>.Forbidden("Apenas o proprietário da organização pode criar times.");
        }

        if (request.ParentTeamId.HasValue)
        {
            var parentTeam = await teamRepository.GetByIdAsync(request.ParentTeamId.Value, cancellationToken);
            if (parentTeam is null)
            {
                return Result<Team>.NotFound("Time pai não encontrado.");
            }

            if (parentTeam.WorkspaceId != request.WorkspaceId)
            {
                return Result<Team>.Failure("O time pai deve pertencer ao mesmo workspace.");
            }
        }

        var leaderValidation = await ValidateLeaderAsync(request.LeaderId, workspace.OrganizationId, cancellationToken);
        if (leaderValidation is not null)
        {
            return leaderValidation;
        }

        try
        {
            var team = Team.Create(
                Guid.NewGuid(),
                workspace.OrganizationId,
                request.WorkspaceId,
                request.Name,
                request.LeaderId,
                request.ParentTeamId);

            team.CollaboratorTeams.Add(new CollaboratorTeam
            {
                CollaboratorId = request.LeaderId,
                TeamId = team.Id,
                AssignedAt = DateTime.UtcNow
            });

            await teamRepository.AddAsync(team, cancellationToken);
            await teamRepository.SaveChangesAsync(cancellationToken);

            return Result<Team>.Success(team);
        }
        catch (DomainInvariantException ex)
        {
            return Result<Team>.Failure(ex.Message, ErrorType.Validation);
        }
    }

    public async Task<Result<Team>> UpdateAsync(
        ClaimsPrincipal user,
        Guid id,
        UpdateTeamRequest request,
        CancellationToken cancellationToken = default)
    {
        var team = await teamRepository.GetByIdWithCollaboratorTeamsAsync(id, cancellationToken);
        if (team is null)
        {
            return Result<Team>.NotFound("Time não encontrado.");
        }

        var canUpdate = await authorizationGateway.CanWriteOrganizationAsync(user, team.OrganizationId, cancellationToken);
        if (!canUpdate)
        {
            return Result<Team>.Forbidden("Você não tem permissão para atualizar este time.");
        }

        if (request.ParentTeamId.HasValue && request.ParentTeamId != team.ParentTeamId)
        {
            if (request.ParentTeamId == id)
            {
                return Result<Team>.Failure("Um time não pode ser seu próprio pai.");
            }

            var parentTeam = await teamRepository.GetByIdAsync(request.ParentTeamId.Value, cancellationToken);
            if (parentTeam is null)
            {
                return Result<Team>.NotFound("Time pai não encontrado.");
            }

            if (parentTeam.WorkspaceId != team.WorkspaceId)
            {
                return Result<Team>.Failure("O time pai deve pertencer ao mesmo workspace.");
            }
        }

        var leaderValidation = await ValidateLeaderAsync(request.LeaderId, team.OrganizationId, cancellationToken);
        if (leaderValidation is not null)
        {
            return leaderValidation;
        }

        try
        {
            team.Rename(request.Name);
            team.AssignLeader(request.LeaderId);
            team.Reparent(request.ParentTeamId, team.Id);

            if (!team.CollaboratorTeams.Any(ct => ct.CollaboratorId == request.LeaderId))
            {
                team.CollaboratorTeams.Add(new CollaboratorTeam
                {
                    CollaboratorId = request.LeaderId,
                    TeamId = team.Id,
                    AssignedAt = DateTime.UtcNow
                });
            }

            await teamRepository.SaveChangesAsync(cancellationToken);

            return Result<Team>.Success(team);
        }
        catch (DomainInvariantException ex)
        {
            return Result<Team>.Failure(ex.Message, ErrorType.Validation);
        }
    }

    public async Task<Result> DeleteAsync(
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var team = await teamRepository.GetByIdAsync(id, cancellationToken);
        if (team is null)
        {
            return Result.NotFound("Time não encontrado.");
        }

        var canDelete = await authorizationGateway.CanWriteOrganizationAsync(user, team.OrganizationId, cancellationToken);
        if (!canDelete)
        {
            return Result.Forbidden("Você não tem permissão para excluir este time.");
        }

        if (await teamRepository.HasSubTeamsAsync(id, cancellationToken))
        {
            return Result.Failure("Não é possível excluir um time com sub-times. Exclua os sub-times primeiro.", ErrorType.Conflict);
        }

        if (await teamRepository.HasMissionsAsync(id, cancellationToken))
        {
            return Result.Failure(
                "Não é possível excluir o time porque existem missões associadas a ele.",
                ErrorType.Conflict);
        }

        await teamRepository.RemoveAsync(team, cancellationToken);
        await teamRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> UpdateCollaboratorsAsync(
        ClaimsPrincipal user,
        Guid id,
        UpdateTeamCollaboratorsRequest request,
        CancellationToken cancellationToken = default)
    {
        var team = await teamRepository.GetByIdWithCollaboratorTeamsAsync(id, cancellationToken);
        if (team is null)
        {
            return Result.NotFound("Time não encontrado.");
        }

        var canManage = await authorizationGateway.IsOrganizationOwnerAsync(user, team.OrganizationId, cancellationToken);
        if (!canManage)
        {
            return Result.Forbidden("Apenas o proprietário da organização pode atribuir colaboradores.");
        }

        var distinctCollaboratorIds = request.CollaboratorIds.Distinct().ToList();

        if (!distinctCollaboratorIds.Contains(team.LeaderId))
        {
            return Result.Failure("O líder da equipe deve estar incluído na lista de membros.", ErrorType.Validation);
        }

        if (distinctCollaboratorIds.Count > 0)
        {
            var validCount = await collaboratorRepository.CountByIdsAndOrganizationAsync(
                distinctCollaboratorIds, team.OrganizationId, cancellationToken);

            if (validCount != distinctCollaboratorIds.Count)
            {
                return Result.Failure("Um ou mais colaboradores são inválidos ou pertencem a outra organização.", ErrorType.Validation);
            }
        }

        team.CollaboratorTeams.Clear();

        foreach (var collaboratorId in distinctCollaboratorIds)
        {
            team.CollaboratorTeams.Add(new CollaboratorTeam
            {
                CollaboratorId = collaboratorId,
                TeamId = id,
                AssignedAt = DateTime.UtcNow
            });
        }

        await teamRepository.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task<Result<Team>?> ValidateLeaderAsync(Guid leaderId, Guid organizationId, CancellationToken cancellationToken)
    {
        var leader = await collaboratorRepository.GetByIdAsync(leaderId, cancellationToken);
        if (leader is null)
        {
            return Result<Team>.NotFound("Líder não encontrado.");
        }

        if (leader.Role != CollaboratorRole.Leader)
        {
            return Result<Team>.Failure("O colaborador selecionado como líder deve ter o perfil de Líder.", ErrorType.Validation);
        }

        if (leader.OrganizationId != organizationId)
        {
            return Result<Team>.Failure("O líder deve pertencer à mesma organização do time.", ErrorType.Validation);
        }

        return null;
    }
}
