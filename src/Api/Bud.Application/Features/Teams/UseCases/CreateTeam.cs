using System.Security.Claims;
using Bud.Application.Common;
using Bud.Application.Ports;
using Microsoft.Extensions.Logging;

namespace Bud.Application.Features.Teams.UseCases;

public sealed record CreateTeamCommand(string Name, Guid WorkspaceId, Guid LeaderId, Guid? ParentTeamId);

public sealed partial class CreateTeam(
    ITeamRepository teamRepository,
    IWorkspaceRepository workspaceRepository,
    ICollaboratorRepository collaboratorRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    ILogger<CreateTeam> logger,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<Team>> ExecuteAsync(
        ClaimsPrincipal user,
        CreateTeamCommand command,
        CancellationToken cancellationToken = default)
    {
        LogCreatingTeam(logger, command.Name, command.WorkspaceId);

        var workspace = await workspaceRepository.GetByIdAsync(command.WorkspaceId, cancellationToken);
        if (workspace is null)
        {
            LogTeamCreationFailed(logger, command.Name, "Workspace not found");
            return Result<Team>.NotFound(UserErrorMessages.WorkspaceNotFound);
        }

        var canCreate = await authorizationGateway.IsOrganizationOwnerAsync(user, workspace.OrganizationId, cancellationToken);
        if (!canCreate)
        {
            LogTeamCreationFailed(logger, command.Name, "Forbidden");
            return Result<Team>.Forbidden(UserErrorMessages.TeamCreateForbidden);
        }

        if (command.ParentTeamId.HasValue)
        {
            var parentTeam = await teamRepository.GetByIdAsync(command.ParentTeamId.Value, cancellationToken);
            if (parentTeam is null)
            {
                LogTeamCreationFailed(logger, command.Name, "Parent team not found");
                return Result<Team>.NotFound(UserErrorMessages.ParentTeamNotFound);
            }

            if (parentTeam.WorkspaceId != command.WorkspaceId)
            {
                LogTeamCreationFailed(logger, command.Name, "Parent team belongs to different workspace");
                return Result<Team>.Failure(UserErrorMessages.TeamParentMustBeSameWorkspace);
            }
        }

        var leaderValidation = await CollaboratorLeadershipPolicy.ValidateLeaderForOrganizationAsync<Team>(
            collaboratorRepository,
            command.LeaderId,
            workspace.OrganizationId,
            cancellationToken);
        if (leaderValidation is not null)
        {
            LogTeamCreationFailed(logger, command.Name, "Leader validation failed");
            return leaderValidation;
        }

        try
        {
            var team = Team.Create(
                Guid.NewGuid(),
                workspace.OrganizationId,
                command.WorkspaceId,
                command.Name,
                command.LeaderId,
                command.ParentTeamId);

            team.CollaboratorTeams.Add(new CollaboratorTeam
            {
                CollaboratorId = command.LeaderId,
                TeamId = team.Id,
                AssignedAt = DateTime.UtcNow
            });

            await teamRepository.AddAsync(team, cancellationToken);
            await unitOfWork.CommitAsync(teamRepository.SaveChangesAsync, cancellationToken);

            LogTeamCreated(logger, team.Id, team.Name);
            return Result<Team>.Success(team);
        }
        catch (DomainInvariantException ex)
        {
            LogTeamCreationFailed(logger, command.Name, ex.Message);
            return Result<Team>.Failure(ex.Message, ErrorType.Validation);
        }
    }

    [LoggerMessage(EventId = 4030, Level = LogLevel.Information, Message = "Creating team '{Name}' in workspace {WorkspaceId}")]
    private static partial void LogCreatingTeam(ILogger logger, string name, Guid workspaceId);

    [LoggerMessage(EventId = 4031, Level = LogLevel.Information, Message = "Team created successfully: {TeamId} - '{Name}'")]
    private static partial void LogTeamCreated(ILogger logger, Guid teamId, string name);

    [LoggerMessage(EventId = 4032, Level = LogLevel.Warning, Message = "Team creation failed for '{Name}': {Reason}")]
    private static partial void LogTeamCreationFailed(ILogger logger, string name, string reason);
}
