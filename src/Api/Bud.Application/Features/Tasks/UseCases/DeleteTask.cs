using Bud.Application.Common;
using Bud.Application.Ports;
using Microsoft.Extensions.Logging;

namespace Bud.Application.Features.Tasks.UseCases;

public sealed partial class DeleteTask(
    ITaskRepository taskRepository,
    ILogger<DeleteTask> logger,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result> ExecuteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        LogDeletingTask(logger, id);

        var task = await taskRepository.GetByIdAsync(id, cancellationToken);
        if (task is null)
        {
            LogTaskDeletionFailed(logger, id, "Not found");
            return Result.NotFound(UserErrorMessages.TaskNotFound);
        }

        await taskRepository.RemoveAsync(task, cancellationToken);
        await unitOfWork.CommitAsync(taskRepository.SaveChangesAsync, cancellationToken);

        LogTaskDeleted(logger, id);
        return Result.Success();
    }

    [LoggerMessage(EventId = 4086, Level = LogLevel.Information, Message = "Deleting task {TaskId}")]
    private static partial void LogDeletingTask(ILogger logger, Guid taskId);

    [LoggerMessage(EventId = 4087, Level = LogLevel.Information, Message = "Task deleted successfully: {TaskId}")]
    private static partial void LogTaskDeleted(ILogger logger, Guid taskId);

    [LoggerMessage(EventId = 4088, Level = LogLevel.Warning, Message = "Task deletion failed for {TaskId}: {Reason}")]
    private static partial void LogTaskDeletionFailed(ILogger logger, Guid taskId, string reason);
}
