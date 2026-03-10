using System.Security.Claims;
using Bud.Application.Common;
using Bud.Application.Ports;
using Bud.Shared.Contracts;
using Microsoft.Extensions.Logging;

namespace Bud.Application.Collaborators;

public sealed partial class PatchCollaboratorTeams(
    ICollaboratorRepository collaboratorRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    ILogger<PatchCollaboratorTeams> logger,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result> ExecuteAsync(
        ClaimsPrincipal user,
        Guid id,
        PatchCollaboratorTeamsRequest request,
        CancellationToken cancellationToken = default)
    {
        LogPatchingCollaboratorTeams(logger, id);

        var collaborator = await collaboratorRepository.GetByIdWithCollaboratorTeamsAsync(id, cancellationToken);
        if (collaborator is null)
        {
            LogCollaboratorTeamsPatchFailed(logger, id, "Collaborator not found");
            return Result.NotFound(UserErrorMessages.CollaboratorNotFound);
        }

        var canAssign = await authorizationGateway.IsOrganizationOwnerAsync(user, collaborator.OrganizationId, cancellationToken);
        if (!canAssign)
        {
            LogCollaboratorTeamsPatchFailed(logger, id, "Forbidden");
            return Result.Forbidden(UserErrorMessages.CollaboratorAssignTeamsForbidden);
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
                LogCollaboratorTeamsPatchFailed(logger, id, "Invalid teams");
                return Result.Failure(UserErrorMessages.CollaboratorTeamsInvalid, ErrorType.Validation);
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
        LogCollaboratorTeamsPatched(logger, id, distinctTeamIds.Count);
        return Result.Success();
    }

    [LoggerMessage(EventId = 4049, Level = LogLevel.Information, Message = "Patching teams for collaborator {CollaboratorId}")]
    private static partial void LogPatchingCollaboratorTeams(ILogger logger, Guid collaboratorId);

    [LoggerMessage(EventId = 4049, Level = LogLevel.Information, Message = "Collaborator teams patched successfully: {CollaboratorId} with {Count} teams")]
    private static partial void LogCollaboratorTeamsPatched(ILogger logger, Guid collaboratorId, int count);

    [LoggerMessage(EventId = 4049, Level = LogLevel.Warning, Message = "Collaborator teams patch failed for {CollaboratorId}: {Reason}")]
    private static partial void LogCollaboratorTeamsPatchFailed(ILogger logger, Guid collaboratorId, string reason);
}
