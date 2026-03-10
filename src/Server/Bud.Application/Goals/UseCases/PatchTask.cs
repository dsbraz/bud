using System.Security.Claims;
using Bud.Application.Common;
using Bud.Application.Ports;
using Microsoft.Extensions.Logging;

namespace Bud.Application.Goals;

public sealed partial class PatchTask(
    ITaskRepository taskRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    ILogger<PatchTask> logger,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<TaskResponse>> ExecuteAsync(
        ClaimsPrincipal user,
        Guid id,
        PatchTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        LogPatchingTask(logger, id);

        var task = await taskRepository.GetByIdAsync(id, cancellationToken);
        if (task is null)
        {
            LogTaskPatchFailed(logger, id, "Not found");
            return Result<TaskResponse>.NotFound(UserErrorMessages.TaskNotFound);
        }

        var canUpdate = await authorizationGateway.CanAccessTenantOrganizationAsync(user, task.OrganizationId, cancellationToken);
        if (!canUpdate)
        {
            LogTaskPatchFailed(logger, id, "Forbidden");
            return Result<TaskResponse>.Forbidden(UserErrorMessages.TaskUpdateForbidden);
        }

        try
        {
            var name = request.Name.HasValue ? (request.Name.Value ?? task.Name) : task.Name;
            var description = request.Description.HasValue ? request.Description.Value : task.Description;
            var state = request.State.HasValue ? request.State.Value : task.State;
            var dueDate = request.DueDate.HasValue ? request.DueDate.Value : task.DueDate;

            task.UpdateDetails(name, description, state, dueDate);
            await unitOfWork.CommitAsync(taskRepository.SaveChangesAsync, cancellationToken);

            LogTaskPatched(logger, id, task.Name);
            return Result<TaskResponse>.Success(task.ToResponse());
        }
        catch (DomainInvariantException ex)
        {
            LogTaskPatchFailed(logger, id, ex.Message);
            return Result<TaskResponse>.Failure(ex.Message, ErrorType.Validation);
        }
    }

    [LoggerMessage(EventId = 4083, Level = LogLevel.Information, Message = "Patching task {TaskId}")]
    private static partial void LogPatchingTask(ILogger logger, Guid taskId);

    [LoggerMessage(EventId = 4084, Level = LogLevel.Information, Message = "Task patched successfully: {TaskId} - '{Name}'")]
    private static partial void LogTaskPatched(ILogger logger, Guid taskId, string name);

    [LoggerMessage(EventId = 4085, Level = LogLevel.Warning, Message = "Task patch failed for {TaskId}: {Reason}")]
    private static partial void LogTaskPatchFailed(ILogger logger, Guid taskId, string reason);
}
