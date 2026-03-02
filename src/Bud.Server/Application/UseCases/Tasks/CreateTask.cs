using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Authorization;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Shared.Contracts.Requests;
using Microsoft.Extensions.Logging;

namespace Bud.Server.Application.UseCases.Tasks;

public sealed partial class CreateTask(
    ITaskRepository taskRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    ILogger<CreateTask> logger,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<TaskResponse>> ExecuteAsync(
        ClaimsPrincipal user,
        CreateTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        LogCreatingTask(logger, request.Name, request.GoalId);

        var goal = await taskRepository.GetGoalByIdAsync(request.GoalId, cancellationToken);
        if (goal is null)
        {
            LogTaskCreationFailed(logger, request.Name, "Goal not found");
            return Result<TaskResponse>.NotFound(UserErrorMessages.GoalNotFound);
        }

        var canCreate = await authorizationGateway.CanAccessTenantOrganizationAsync(user, goal.OrganizationId, cancellationToken);
        if (!canCreate)
        {
            LogTaskCreationFailed(logger, request.Name, "Forbidden");
            return Result<TaskResponse>.Forbidden(UserErrorMessages.TaskCreateForbidden);
        }

        try
        {
            var task = GoalTask.Create(
                Guid.NewGuid(),
                goal.OrganizationId,
                request.GoalId,
                request.Name,
                request.Description,
                request.State,
                request.DueDate);

            await taskRepository.AddAsync(task, cancellationToken);
            await unitOfWork.CommitAsync(taskRepository.SaveChangesAsync, cancellationToken);

            LogTaskCreated(logger, task.Id, task.Name);
            return Result<TaskResponse>.Success(task.ToResponse());
        }
        catch (DomainInvariantException ex)
        {
            LogTaskCreationFailed(logger, request.Name, ex.Message);
            return Result<TaskResponse>.Failure(ex.Message, ErrorType.Validation);
        }
    }

    [LoggerMessage(EventId = 4080, Level = LogLevel.Information, Message = "Creating task '{Name}' for goal {GoalId}")]
    private static partial void LogCreatingTask(ILogger logger, string name, Guid goalId);

    [LoggerMessage(EventId = 4081, Level = LogLevel.Information, Message = "Task created successfully: {TaskId} - '{Name}'")]
    private static partial void LogTaskCreated(ILogger logger, Guid taskId, string name);

    [LoggerMessage(EventId = 4082, Level = LogLevel.Warning, Message = "Task creation failed for '{Name}': {Reason}")]
    private static partial void LogTaskCreationFailed(ILogger logger, string name, string reason);
}
