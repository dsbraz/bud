using System.Security.Claims;
using Bud.Application.Common;
using Bud.Application.Mapping;
using Bud.Application.Ports;
using Bud.Domain.Model;
using Bud.Domain.Repositories;
using Bud.Shared.Contracts;
using Microsoft.Extensions.Logging;

namespace Bud.Application.UseCases.Teams;

public sealed partial class PatchTeamCollaborators(
    ITeamRepository teamRepository,
    ICollaboratorRepository collaboratorRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    ILogger<PatchTeamCollaborators> logger,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result> ExecuteAsync(
        ClaimsPrincipal user,
        Guid id,
        PatchTeamCollaboratorsRequest request,
        CancellationToken cancellationToken = default)
    {
        LogPatchingTeamCollaborators(logger, id);

        var team = await teamRepository.GetByIdWithCollaboratorTeamsAsync(id, cancellationToken);
        if (team is null)
        {
            LogTeamCollaboratorsPatchFailed(logger, id, "Team not found");
            return Result.NotFound(UserErrorMessages.TeamNotFound);
        }

        var canManage = await authorizationGateway.IsOrganizationOwnerAsync(user, team.OrganizationId, cancellationToken);
        if (!canManage)
        {
            LogTeamCollaboratorsPatchFailed(logger, id, "Forbidden");
            return Result.Forbidden(UserErrorMessages.TeamAssignForbidden);
        }

        var distinctCollaboratorIds = request.CollaboratorIds.Distinct().ToList();

        if (!distinctCollaboratorIds.Contains(team.LeaderId))
        {
            LogTeamCollaboratorsPatchFailed(logger, id, "Leader not in members list");
            return Result.Failure(UserErrorMessages.TeamLeaderMustBeMember, ErrorType.Validation);
        }

        if (distinctCollaboratorIds.Count > 0)
        {
            var validCount = await collaboratorRepository.CountByIdsAndOrganizationAsync(
                distinctCollaboratorIds,
                team.OrganizationId,
                cancellationToken);

            if (validCount != distinctCollaboratorIds.Count)
            {
                LogTeamCollaboratorsPatchFailed(logger, id, "Invalid collaborators");
                return Result.Failure(UserErrorMessages.TeamMembersInvalid, ErrorType.Validation);
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
        LogTeamCollaboratorsPatched(logger, id, distinctCollaboratorIds.Count);
        return Result.Success();
    }

    [LoggerMessage(EventId = 4039, Level = LogLevel.Information, Message = "Patching collaborators for team {TeamId}")]
    private static partial void LogPatchingTeamCollaborators(ILogger logger, Guid teamId);

    [LoggerMessage(EventId = 4039, Level = LogLevel.Information, Message = "Team collaborators patched successfully: {TeamId} with {Count} members")]
    private static partial void LogTeamCollaboratorsPatched(ILogger logger, Guid teamId, int count);

    [LoggerMessage(EventId = 4039, Level = LogLevel.Warning, Message = "Team collaborators patch failed for {TeamId}: {Reason}")]
    private static partial void LogTeamCollaboratorsPatchFailed(ILogger logger, Guid teamId, string reason);
}
