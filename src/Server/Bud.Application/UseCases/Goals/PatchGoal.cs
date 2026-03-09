using System.Security.Claims;
using Bud.Application.Common;
using Bud.Application.Policies;
using Bud.Application.Ports;
using Bud.Domain.Model;
using Bud.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Bud.Application.UseCases.Goals;

public sealed partial class PatchGoal(
    IGoalRepository goalRepository,
    ICollaboratorRepository collaboratorRepository,
    ITenantProvider tenantProvider,
    IApplicationAuthorizationGateway authorizationGateway,
    ILogger<PatchGoal> logger,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<Goal>> ExecuteAsync(
        ClaimsPrincipal user,
        Guid id,
        PatchGoalRequest request,
        CancellationToken cancellationToken = default)
    {
        LogPatchingGoal(logger, id);

        var goal = await goalRepository.GetByIdAsync(id, cancellationToken);
        if (goal is null)
        {
            LogGoalPatchFailed(logger, id, "Not found");
            return Result<Goal>.NotFound(UserErrorMessages.GoalNotFound);
        }

        var canUpdate = await authorizationGateway.CanAccessTenantOrganizationAsync(user, goal.OrganizationId, cancellationToken);
        if (!canUpdate)
        {
            LogGoalPatchFailed(logger, id, "Forbidden");
            return Result<Goal>.Forbidden(UserErrorMessages.GoalUpdateForbidden);
        }

        if (goal.ParentId.HasValue && request.StartDate.HasValue)
        {
            var parentGoal = await goalRepository.GetByIdReadOnlyAsync(goal.ParentId.Value, cancellationToken);
            if (parentGoal is not null)
            {
                var violation = GoalDateRangePolicy.ValidateChildStartDate<Goal>(
                    UtcDateTimeNormalizer.Normalize(request.StartDate.Value), parentGoal.StartDate);
                if (violation is not null)
                {
                    LogGoalPatchFailed(logger, id, violation.Error!);
                    return violation;
                }
            }
        }

        try
        {
            var status = request.Status.HasValue ? request.Status.Value : goal.Status;
            var name = request.Name.HasValue ? (request.Name.Value ?? goal.Name) : goal.Name;
            var description = request.Description.HasValue ? request.Description.Value : goal.Description;
            var dimension = request.Dimension.HasValue ? request.Dimension.Value : goal.Dimension;
            var startDate = request.StartDate.HasValue ? request.StartDate.Value : goal.StartDate;
            var endDate = request.EndDate.HasValue ? request.EndDate.Value : goal.EndDate;

            goal.UpdateDetails(
                name,
                description,
                dimension,
                UtcDateTimeNormalizer.Normalize(startDate),
                UtcDateTimeNormalizer.Normalize(endDate),
                status);

            if (request.CollaboratorId.HasValue)
            {
                var newCollaboratorId = request.CollaboratorId.Value;
                if (newCollaboratorId.HasValue)
                {
                    var collaborator = await collaboratorRepository.GetByIdAsync(newCollaboratorId.Value, cancellationToken);
                    if (collaborator is null)
                    {
                        LogGoalPatchFailed(logger, id, UserErrorMessages.CollaboratorNotFound);
                        return Result<Goal>.NotFound(UserErrorMessages.CollaboratorNotFound);
                    }

                    if (collaborator.OrganizationId != goal.OrganizationId)
                    {
                        LogGoalPatchFailed(logger, id, "Collaborator belongs to different organization");
                        return Result<Goal>.Forbidden(UserErrorMessages.GoalUpdateForbidden);
                    }
                }

                goal.CollaboratorId = newCollaboratorId;
            }

            goal.MarkAsUpdated(tenantProvider.CollaboratorId);
            await unitOfWork.CommitAsync(goalRepository.SaveChangesAsync, cancellationToken);

            LogGoalPatched(logger, id, goal.Name);
            return Result<Goal>.Success(goal);
        }
        catch (DomainInvariantException ex)
        {
            LogGoalPatchFailed(logger, id, ex.Message);
            return Result<Goal>.Failure(ex.Message, ErrorType.Validation);
        }
    }

    [LoggerMessage(EventId = 4003, Level = LogLevel.Information, Message = "Patching goal {GoalId}")]
    private static partial void LogPatchingGoal(ILogger logger, Guid goalId);

    [LoggerMessage(EventId = 4004, Level = LogLevel.Information, Message = "Goal patched successfully: {GoalId} - '{Name}'")]
    private static partial void LogGoalPatched(ILogger logger, Guid goalId, string name);

    [LoggerMessage(EventId = 4005, Level = LogLevel.Warning, Message = "Goal patch failed for {GoalId}: {Reason}")]
    private static partial void LogGoalPatchFailed(ILogger logger, Guid goalId, string reason);
}
