using System.Security.Claims;
using Bud.Application.Common;
using Bud.Application.Ports;
using Bud.Shared.Contracts;
using Microsoft.Extensions.Logging;

namespace Bud.Application.Teams;

public sealed partial class PatchTeam(
    ITeamRepository teamRepository,
    ICollaboratorRepository collaboratorRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    ILogger<PatchTeam> logger,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<Team>> ExecuteAsync(
        ClaimsPrincipal user,
        Guid id,
        PatchTeamRequest request,
        CancellationToken cancellationToken = default)
    {
        LogPatchingTeam(logger, id);

        var team = await teamRepository.GetByIdWithCollaboratorTeamsAsync(id, cancellationToken);
        if (team is null)
        {
            LogTeamPatchFailed(logger, id, "Not found");
            return Result<Team>.NotFound(UserErrorMessages.TeamNotFound);
        }

        var canUpdate = await authorizationGateway.CanWriteOrganizationAsync(user, team.OrganizationId, cancellationToken);
        if (!canUpdate)
        {
            LogTeamPatchFailed(logger, id, "Forbidden");
            return Result<Team>.Forbidden(UserErrorMessages.TeamUpdateForbidden);
        }

        var requestedParentTeamId = request.ParentTeamId.HasValue ? request.ParentTeamId.Value : team.ParentTeamId;
        var requestedLeaderId = request.LeaderId.HasValue ? request.LeaderId.Value : team.LeaderId;
        var requestedName = request.Name.HasValue ? (request.Name.Value ?? team.Name) : team.Name;

        if (request.ParentTeamId.HasValue && requestedParentTeamId != team.ParentTeamId)
        {
            if (requestedParentTeamId.HasValue && requestedParentTeamId.Value == id)
            {
                LogTeamPatchFailed(logger, id, "Team cannot be its own parent");
                return Result<Team>.Failure(UserErrorMessages.TeamSelfParentForbidden);
            }

            var parentTeam = requestedParentTeamId.HasValue
                ? await teamRepository.GetByIdAsync(requestedParentTeamId.Value, cancellationToken)
                : null;
            if (parentTeam is null)
            {
                LogTeamPatchFailed(logger, id, "Parent team not found");
                return Result<Team>.NotFound(UserErrorMessages.ParentTeamNotFound);
            }

            if (parentTeam.WorkspaceId != team.WorkspaceId)
            {
                LogTeamPatchFailed(logger, id, "Parent team belongs to different workspace");
                return Result<Team>.Failure(UserErrorMessages.TeamParentMustBeSameWorkspace);
            }
        }

        var leaderValidation = await CollaboratorLeadershipPolicy.ValidateLeaderForOrganizationAsync<Team>(
            collaboratorRepository,
            requestedLeaderId,
            team.OrganizationId,
            cancellationToken);
        if (leaderValidation is not null)
        {
            LogTeamPatchFailed(logger, id, "Leader validation failed");
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

            LogTeamPatched(logger, id, team.Name);
            return Result<Team>.Success(team);
        }
        catch (DomainInvariantException ex)
        {
            LogTeamPatchFailed(logger, id, ex.Message);
            return Result<Team>.Failure(ex.Message, ErrorType.Validation);
        }
    }

    [LoggerMessage(EventId = 4033, Level = LogLevel.Information, Message = "Patching team {TeamId}")]
    private static partial void LogPatchingTeam(ILogger logger, Guid teamId);

    [LoggerMessage(EventId = 4034, Level = LogLevel.Information, Message = "Team patched successfully: {TeamId} - '{Name}'")]
    private static partial void LogTeamPatched(ILogger logger, Guid teamId, string name);

    [LoggerMessage(EventId = 4035, Level = LogLevel.Warning, Message = "Team patch failed for {TeamId}: {Reason}")]
    private static partial void LogTeamPatchFailed(ILogger logger, Guid teamId, string reason);
}
