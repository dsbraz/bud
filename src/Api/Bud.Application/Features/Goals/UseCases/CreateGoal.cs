using Bud.Application.Common;
using Bud.Application.Ports;
using Microsoft.Extensions.Logging;

namespace Bud.Application.Features.Goals.UseCases;

public sealed record CreateGoalCommand(
    string Name,
    string? Description,
    string? Dimension,
    DateTime StartDate,
    DateTime EndDate,
    GoalStatus Status,
    Guid? ParentId,
    Guid? CollaboratorId);

public sealed partial class CreateGoal(
    IGoalRepository goalRepository,
    ICollaboratorRepository collaboratorRepository,
    ITenantProvider tenantProvider,
    ILogger<CreateGoal> logger,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<Goal>> ExecuteAsync(
        CreateGoalCommand command,
        CancellationToken cancellationToken = default)
    {
        LogCreatingGoal(logger, command.Name);

        var organizationId = tenantProvider.TenantId;
        if (!organizationId.HasValue)
        {
            LogGoalCreationFailed(logger, command.Name, "Tenant not selected");
            return Result<Goal>.Forbidden(UserErrorMessages.GoalCreateForbidden);
        }

        Goal? parentGoal = null;
        if (command.ParentId.HasValue)
        {
            parentGoal = await goalRepository.GetByIdReadOnlyAsync(command.ParentId.Value, cancellationToken);
            if (parentGoal is null)
            {
                LogGoalCreationFailed(logger, command.Name, UserErrorMessages.ParentGoalNotFound);
                return Result<Goal>.NotFound(UserErrorMessages.ParentGoalNotFound);
            }
        }

        if (parentGoal is not null)
        {
            var violation = GoalDateRangePolicy.ValidateChildStartDate<Goal>(
                UtcDateTimeNormalizer.Normalize(command.StartDate), parentGoal.StartDate);
            if (violation is not null)
            {
                LogGoalCreationFailed(logger, command.Name, violation.Error!);
                return violation;
            }
        }

        try
        {
            var goal = Goal.Create(
                Guid.NewGuid(),
                organizationId.Value,
                command.Name,
                command.Description,
                command.Dimension,
                UtcDateTimeNormalizer.Normalize(command.StartDate),
                UtcDateTimeNormalizer.Normalize(command.EndDate),
                command.Status,
                parentGoal?.Id,
                tenantProvider.CollaboratorId);

            if (command.CollaboratorId.HasValue)
            {
                var collaborator = await collaboratorRepository.GetByIdAsync(command.CollaboratorId.Value, cancellationToken);
                if (collaborator is null)
                {
                    LogGoalCreationFailed(logger, command.Name, UserErrorMessages.CollaboratorNotFound);
                    return Result<Goal>.NotFound(UserErrorMessages.CollaboratorNotFound);
                }

                if (collaborator.OrganizationId != organizationId.Value)
                {
                    LogGoalCreationFailed(logger, command.Name, "Collaborator belongs to different organization");
                    return Result<Goal>.Forbidden(UserErrorMessages.GoalCreateForbidden);
                }

                goal.CollaboratorId = command.CollaboratorId.Value;
            }

            await goalRepository.AddAsync(goal, cancellationToken);
            await unitOfWork.CommitAsync(goalRepository.SaveChangesAsync, cancellationToken);

            LogGoalCreated(logger, goal.Id, goal.Name);
            return Result<Goal>.Success(goal);
        }
        catch (DomainInvariantException ex)
        {
            LogGoalCreationFailed(logger, command.Name, ex.Message);
            return Result<Goal>.Failure(ex.Message, ErrorType.Validation);
        }
    }

    [LoggerMessage(EventId = 4000, Level = LogLevel.Information, Message = "Creating goal '{Name}'")]
    private static partial void LogCreatingGoal(ILogger logger, string name);

    [LoggerMessage(EventId = 4001, Level = LogLevel.Information, Message = "Goal created successfully: {GoalId} - '{Name}'")]
    private static partial void LogGoalCreated(ILogger logger, Guid goalId, string name);

    [LoggerMessage(EventId = 4002, Level = LogLevel.Warning, Message = "Goal creation failed for '{Name}': {Reason}")]
    private static partial void LogGoalCreationFailed(ILogger logger, string name, string reason);
}
