using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Authorization;
using Bud.Server.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Bud.Server.Application.UseCases.Goals;

public sealed partial class DeleteGoal(
    IGoalRepository goalRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    ILogger<DeleteGoal> logger,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result> ExecuteAsync(
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        LogDeletingGoal(logger, id);

        var goal = await goalRepository.GetByIdAsync(id, cancellationToken);
        if (goal is null)
        {
            LogGoalDeletionFailed(logger, id, "Not found");
            return Result.NotFound("Meta não encontrada.");
        }

        var canDelete = await authorizationGateway.CanAccessTenantOrganizationAsync(user, goal.OrganizationId, cancellationToken);
        if (!canDelete)
        {
            LogGoalDeletionFailed(logger, id, "Forbidden");
            return Result.Forbidden("Você não tem permissão para excluir metas nesta organização.");
        }

        goal.MarkAsDeleted();
        await goalRepository.RemoveAsync(goal, cancellationToken);
        await unitOfWork.CommitAsync(goalRepository.SaveChangesAsync, cancellationToken);

        LogGoalDeleted(logger, id);
        return Result.Success();
    }

    [LoggerMessage(EventId = 4006, Level = LogLevel.Information, Message = "Deleting goal {GoalId}")]
    private static partial void LogDeletingGoal(ILogger logger, Guid goalId);

    [LoggerMessage(EventId = 4007, Level = LogLevel.Information, Message = "Goal deleted successfully: {GoalId}")]
    private static partial void LogGoalDeleted(ILogger logger, Guid goalId);

    [LoggerMessage(EventId = 4008, Level = LogLevel.Warning, Message = "Goal deletion failed for {GoalId}: {Reason}")]
    private static partial void LogGoalDeletionFailed(ILogger logger, Guid goalId, string reason);
}
