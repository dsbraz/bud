using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Authorization;
using Bud.Server.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Bud.Server.Application.UseCases.Objectives;

public sealed partial class DeleteObjective(
    IObjectiveRepository objectiveRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    ILogger<DeleteObjective> logger,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result> ExecuteAsync(
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        LogDeletingObjective(logger, id);

        var objective = await objectiveRepository.GetByIdAsync(id, cancellationToken);

        if (objective is null)
        {
            LogObjectiveDeletionFailed(logger, id, "Not found");
            return Result.NotFound("Objetivo não encontrado.");
        }

        var canDelete = await authorizationGateway.CanAccessTenantOrganizationAsync(user, objective.OrganizationId, cancellationToken);
        if (!canDelete)
        {
            LogObjectiveDeletionFailed(logger, id, "Forbidden");
            return Result.Forbidden("Você não tem permissão para excluir objetivos nesta missão.");
        }

        var trackedObjective = await objectiveRepository.GetByIdForUpdateAsync(id, cancellationToken);
        await objectiveRepository.RemoveAsync(trackedObjective!, cancellationToken);
        await unitOfWork.CommitAsync(objectiveRepository.SaveChangesAsync, cancellationToken);

        LogObjectiveDeleted(logger, id);
        return Result.Success();
    }

    [LoggerMessage(EventId = 4087, Level = LogLevel.Information, Message = "Deleting objective {ObjectiveId}")]
    private static partial void LogDeletingObjective(ILogger logger, Guid objectiveId);

    [LoggerMessage(EventId = 4088, Level = LogLevel.Information, Message = "Objective deleted successfully: {ObjectiveId}")]
    private static partial void LogObjectiveDeleted(ILogger logger, Guid objectiveId);

    [LoggerMessage(EventId = 4089, Level = LogLevel.Warning, Message = "Objective deletion failed for {ObjectiveId}: {Reason}")]
    private static partial void LogObjectiveDeletionFailed(ILogger logger, Guid objectiveId, string reason);
}
