using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Domain.ReadModels;
using Bud.Server.Authorization;
using Bud.Server.Domain.Abstractions;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.UseCases.Teams;

public sealed class PatchTeamCollaborators(
    ITeamRepository teamRepository,
    ICollaboratorRepository collaboratorRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result> ExecuteAsync(
        ClaimsPrincipal user,
        Guid id,
        PatchTeamCollaboratorsRequest request,
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
                distinctCollaboratorIds,
                team.OrganizationId,
                cancellationToken);

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

        await unitOfWork.CommitAsync(teamRepository.SaveChangesAsync, cancellationToken);
        return Result.Success();
    }
}

