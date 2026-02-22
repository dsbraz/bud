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

        var requestedParentTeamId = request.ParentTeamId.HasValue ? request.ParentTeamId.Value : team.ParentTeamId;
        var requestedLeaderId = request.LeaderId.HasValue ? request.LeaderId.Value : team.LeaderId;
        var requestedName = request.Name.HasValue ? (request.Name.Value ?? team.Name) : team.Name;

        if (request.ParentTeamId.HasValue && requestedParentTeamId != team.ParentTeamId)
        {
            if (requestedParentTeamId.HasValue && requestedParentTeamId.Value == id)
            {
                return Result<Team>.Failure("Um time não pode ser seu próprio pai.");
            }

            var parentTeam = requestedParentTeamId.HasValue
                ? await teamRepository.GetByIdAsync(requestedParentTeamId.Value, cancellationToken)
                : null;
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
            requestedLeaderId,
            team.OrganizationId,
            cancellationToken);
        if (leaderValidation is not null)
        {
            return leaderValidation;
        }

        try
        {
            team.Rename(requestedName);
            team.AssignLeader(requestedLeaderId);
            team.Reparent(requestedParentTeamId, team.Id);

            if (!team.CollaboratorTeams.Any(ct => ct.CollaboratorId == requestedLeaderId))
            {
                team.CollaboratorTeams.Add(new CollaboratorTeam
                {
                    CollaboratorId = requestedLeaderId,
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
