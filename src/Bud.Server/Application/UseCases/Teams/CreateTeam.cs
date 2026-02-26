using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Authorization;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Shared.Contracts;
using Microsoft.Extensions.Logging;

namespace Bud.Server.Application.UseCases.Teams;

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
        CreateTeamRequest request,
        CancellationToken cancellationToken = default)
    {
        LogCreatingTeam(logger, request.Name, request.WorkspaceId);

        var workspace = await workspaceRepository.GetByIdAsync(request.WorkspaceId, cancellationToken);
        if (workspace is null)
        {
            LogTeamCreationFailed(logger, request.Name, "Workspace not found");
            return Result<Team>.NotFound("Workspace não encontrado.");
        }

        var canCreate = await authorizationGateway.IsOrganizationOwnerAsync(user, workspace.OrganizationId, cancellationToken);
        if (!canCreate)
        {
            LogTeamCreationFailed(logger, request.Name, "Forbidden");
            return Result<Team>.Forbidden("Apenas o proprietário da organização pode criar times.");
        }

        if (request.ParentTeamId.HasValue)
        {
            var parentTeam = await teamRepository.GetByIdAsync(request.ParentTeamId.Value, cancellationToken);
            if (parentTeam is null)
            {
                LogTeamCreationFailed(logger, request.Name, "Parent team not found");
                return Result<Team>.NotFound("Time pai não encontrado.");
            }

            if (parentTeam.WorkspaceId != request.WorkspaceId)
            {
                LogTeamCreationFailed(logger, request.Name, "Parent team belongs to different workspace");
                return Result<Team>.Failure("O time pai deve pertencer ao mesmo workspace.");
            }
        }

        var leaderValidation = await TeamLeaderValidation.ValidateAsync(
            collaboratorRepository,
            request.LeaderId,
            workspace.OrganizationId,
            cancellationToken);
        if (leaderValidation is not null)
        {
            LogTeamCreationFailed(logger, request.Name, "Leader validation failed");
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
            await unitOfWork.CommitAsync(teamRepository.SaveChangesAsync, cancellationToken);

            LogTeamCreated(logger, team.Id, team.Name);
            return Result<Team>.Success(team);
        }
        catch (DomainInvariantException ex)
        {
            LogTeamCreationFailed(logger, request.Name, ex.Message);
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
