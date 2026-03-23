using Bud.Application.Common;
using Bud.Application.Ports;
using Microsoft.Extensions.Logging;

namespace Bud.Application.Features.Goals.UseCases;

public sealed record PatchGoalCommand(
    Optional<string> Name,
    Optional<string?> Description,
    Optional<string?> Dimension,
    Optional<DateTime> StartDate,
    Optional<DateTime> EndDate,
    Optional<GoalStatus> Status,
    Optional<Guid?> CollaboratorId);

public sealed partial class PatchGoal(
    IGoalRepository goalRepository,
    ICollaboratorRepository collaboratorRepository,
    ITenantProvider tenantProvider,
    ILogger<PatchGoal> logger,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<Goal>> ExecuteAsync(
        Guid id,
        PatchGoalCommand command,
        CancellationToken cancellationToken = default)
    {
        LogPatchingGoal(logger, id);

        var goal = await goalRepository.GetByIdAsync(id, cancellationToken);
        if (goal is null)
        {
            LogGoalPatchFailed(logger, id, "Not found");
            return Result<Goal>.NotFound(UserErrorMessages.GoalNotFound);
        }

        if (goal.ParentId.HasValue && command.StartDate.HasValue)
        {
            var parentGoal = await goalRepository.GetByIdReadOnlyAsync(goal.ParentId.Value, cancellationToken);
            if (parentGoal is not null)
            {
                var violation = GoalDateRangePolicy.ValidateChildStartDate<Goal>(
                    UtcDateTimeNormalizer.Normalize(command.StartDate.Value), parentGoal.StartDate);
                if (violation is not null)
                {
                    LogGoalPatchFailed(logger, id, violation.Error!);
                    return violation;
                }
            }
        }

        try
        {
            var status = command.Status.HasValue ? command.Status.Value : goal.Status;
            var name = command.Name.HasValue ? (command.Name.Value ?? goal.Name) : goal.Name;
            var description = command.Description.HasValue ? command.Description.Value : goal.Description;
            var dimension = command.Dimension.HasValue ? command.Dimension.Value : goal.Dimension;
            var startDate = command.StartDate.HasValue ? command.StartDate.Value : goal.StartDate;
            var endDate = command.EndDate.HasValue ? command.EndDate.Value : goal.EndDate;

            goal.UpdateDetails(
                name,
                description,
                dimension,
                UtcDateTimeNormalizer.Normalize(startDate),
                UtcDateTimeNormalizer.Normalize(endDate),
                status);

            if (command.CollaboratorId.HasValue)
            {
                var newCollaboratorId = command.CollaboratorId.Value;
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
