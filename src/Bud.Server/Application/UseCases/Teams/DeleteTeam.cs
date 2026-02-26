using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Authorization;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Shared.Contracts;
using Microsoft.Extensions.Logging;

namespace Bud.Server.Application.UseCases.Teams;

public sealed partial class DeleteTeam(
    ITeamRepository teamRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    ILogger<DeleteTeam> logger,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result> ExecuteAsync(
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        LogDeletingTeam(logger, id);

        var team = await teamRepository.GetByIdAsync(id, cancellationToken);
        if (team is null)
        {
            LogTeamDeletionFailed(logger, id, "Not found");
            return Result.NotFound("Time não encontrado.");
        }

        var canDelete = await authorizationGateway.CanWriteOrganizationAsync(user, team.OrganizationId, cancellationToken);
        if (!canDelete)
        {
            LogTeamDeletionFailed(logger, id, "Forbidden");
            return Result.Forbidden("Você não tem permissão para excluir este time.");
        }

        if (await teamRepository.HasSubTeamsAsync(id, cancellationToken))
        {
            LogTeamDeletionFailed(logger, id, "Has sub-teams");
            return Result.Failure("Não é possível excluir um time com sub-times. Exclua os sub-times primeiro.", ErrorType.Conflict);
        }

        if (await teamRepository.HasMissionsAsync(id, cancellationToken))
        {
            LogTeamDeletionFailed(logger, id, "Has missions");
            return Result.Failure(
                "Não é possível excluir o time porque existem missões associadas a ele.",
                ErrorType.Conflict);
        }

        await teamRepository.RemoveAsync(team, cancellationToken);
        await unitOfWork.CommitAsync(teamRepository.SaveChangesAsync, cancellationToken);

        LogTeamDeleted(logger, id);
        return Result.Success();
    }

    [LoggerMessage(EventId = 4036, Level = LogLevel.Information, Message = "Deleting team {TeamId}")]
    private static partial void LogDeletingTeam(ILogger logger, Guid teamId);

    [LoggerMessage(EventId = 4037, Level = LogLevel.Information, Message = "Team deleted successfully: {TeamId}")]
    private static partial void LogTeamDeleted(ILogger logger, Guid teamId);

    [LoggerMessage(EventId = 4038, Level = LogLevel.Warning, Message = "Team deletion failed for {TeamId}: {Reason}")]
    private static partial void LogTeamDeletionFailed(ILogger logger, Guid teamId, string reason);
}
