using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Authorization;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Shared.Contracts;
using Microsoft.Extensions.Logging;

namespace Bud.Server.Application.UseCases.Teams;

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
            return Result.NotFound("Time não encontrado.");
        }

        var canManage = await authorizationGateway.IsOrganizationOwnerAsync(user, team.OrganizationId, cancellationToken);
        if (!canManage)
        {
            LogTeamCollaboratorsPatchFailed(logger, id, "Forbidden");
            return Result.Forbidden("Apenas o proprietário da organização pode atribuir colaboradores.");
        }

        var distinctCollaboratorIds = request.CollaboratorIds.Distinct().ToList();

        if (!distinctCollaboratorIds.Contains(team.LeaderId))
        {
            LogTeamCollaboratorsPatchFailed(logger, id, "Leader not in members list");
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
                LogTeamCollaboratorsPatchFailed(logger, id, "Invalid collaborators");
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
        LogTeamCollaboratorsPatched(logger, id, distinctCollaboratorIds.Count);
        return Result.Success();
    }

    [LoggerMessage(EventId = 4039, Level = LogLevel.Information, Message = "Patching collaborators for team {TeamId}")]
    private static partial void LogPatchingTeamCollaborators(ILogger logger, Guid teamId);

    [LoggerMessage(EventId = 4040, Level = LogLevel.Information, Message = "Team collaborators patched successfully: {TeamId} with {Count} members")]
    private static partial void LogTeamCollaboratorsPatched(ILogger logger, Guid teamId, int count);

    [LoggerMessage(EventId = 4041, Level = LogLevel.Warning, Message = "Team collaborators patch failed for {TeamId}: {Reason}")]
    private static partial void LogTeamCollaboratorsPatchFailed(ILogger logger, Guid teamId, string reason);
}
