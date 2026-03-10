using System.Security.Claims;
using Bud.Application.Common;
using Bud.Application.Ports;
using Microsoft.Extensions.Logging;

namespace Bud.Application.Goals;

public sealed partial class DeleteGoal(
    IGoalRepository goalRepository,
    ITenantProvider tenantProvider,
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
            return Result.NotFound(UserErrorMessages.GoalNotFound);
        }

        var canDelete = await authorizationGateway.CanAccessTenantOrganizationAsync(user, goal.OrganizationId, cancellationToken);
        if (!canDelete)
        {
            LogGoalDeletionFailed(logger, id, "Forbidden");
            return Result.Forbidden(UserErrorMessages.GoalDeleteForbidden);
        }

        goal.MarkAsDeleted(tenantProvider.CollaboratorId);
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
