using Bud.Application.Common;
using Bud.Application.Ports;
using Microsoft.Extensions.Logging;

namespace Bud.Application.Features.Tasks.UseCases;

public sealed record CreateTaskCommand(
    Guid GoalId,
    string Name,
    string? Description,
    TaskState State,
    DateTime? DueDate);

public sealed partial class CreateTask(
    ITaskRepository taskRepository,
    ILogger<CreateTask> logger,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<GoalTask>> ExecuteAsync(
        CreateTaskCommand command,
        CancellationToken cancellationToken = default)
    {
        LogCreatingTask(logger, command.Name, command.GoalId);

        var goal = await taskRepository.GetGoalByIdAsync(command.GoalId, cancellationToken);
        if (goal is null)
        {
            LogTaskCreationFailed(logger, command.Name, "Goal not found");
            return Result<GoalTask>.NotFound(UserErrorMessages.GoalNotFound);
        }

        try
        {
            var task = GoalTask.Create(
                Guid.NewGuid(),
                goal.OrganizationId,
                command.GoalId,
                command.Name,
                command.Description,
                command.State,
                command.DueDate);

            await taskRepository.AddAsync(task, cancellationToken);
            await unitOfWork.CommitAsync(taskRepository.SaveChangesAsync, cancellationToken);

            LogTaskCreated(logger, task.Id, task.Name);
            return Result<GoalTask>.Success(task);
        }
        catch (DomainInvariantException ex)
        {
            LogTaskCreationFailed(logger, command.Name, ex.Message);
            return Result<GoalTask>.Failure(ex.Message, ErrorType.Validation);
        }
    }

    [LoggerMessage(EventId = 4080, Level = LogLevel.Information, Message = "Creating task '{Name}' for goal {GoalId}")]
    private static partial void LogCreatingTask(ILogger logger, string name, Guid goalId);

    [LoggerMessage(EventId = 4081, Level = LogLevel.Information, Message = "Task created successfully: {TaskId} - '{Name}'")]
    private static partial void LogTaskCreated(ILogger logger, Guid taskId, string name);

    [LoggerMessage(EventId = 4082, Level = LogLevel.Warning, Message = "Task creation failed for '{Name}': {Reason}")]
    private static partial void LogTaskCreationFailed(ILogger logger, string name, string reason);
}
