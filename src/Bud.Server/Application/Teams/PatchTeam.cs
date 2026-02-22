using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Domain.ReadModels;
using Bud.Server.Authorization;
using Bud.Server.Domain.Abstractions;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.Teams;

public sealed class PatchTeam(
    ITeamRepository teamRepository,
    ICollaboratorRepository collaboratorRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<Team>> ExecuteAsync(
        ClaimsPrincipal user,
        Guid id,
        PatchTeamRequest request,
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

        var leaderValidation = await TeamLeaderValidation.ValidateAsync(
            collaboratorRepository,
            request.LeaderId,
            team.OrganizationId,
            cancellationToken);
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

            await unitOfWork.CommitAsync(teamRepository.SaveChangesAsync, cancellationToken);

            return Result<Team>.Success(team);
        }
        catch (DomainInvariantException ex)
        {
            return Result<Team>.Failure(ex.Message, ErrorType.Validation);
        }
    }
}

