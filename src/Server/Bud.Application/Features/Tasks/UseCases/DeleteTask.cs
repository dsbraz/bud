using System.Security.Claims;
using Bud.Application.Common;
using Bud.Application.Ports;
using Microsoft.Extensions.Logging;

namespace Bud.Application.Features.Tasks;

public sealed partial class DeleteTask(
    ITaskRepository taskRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    ILogger<DeleteTask> logger,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result> ExecuteAsync(
        ClaimsPrincipal user,
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

        var canDelete = await authorizationGateway.CanAccessTenantOrganizationAsync(user, task.OrganizationId, cancellationToken);
        if (!canDelete)
        {
            LogTaskDeletionFailed(logger, id, "Forbidden");
            return Result.Forbidden(UserErrorMessages.TaskDeleteForbidden);
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
