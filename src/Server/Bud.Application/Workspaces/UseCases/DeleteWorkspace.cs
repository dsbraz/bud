using System.Security.Claims;
using Bud.Application.Common;
using Bud.Application.Ports;
using Bud.Shared.Contracts;
using Microsoft.Extensions.Logging;

namespace Bud.Application.Workspaces;

public sealed partial class DeleteWorkspace(
    IWorkspaceRepository workspaceRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    ILogger<DeleteWorkspace> logger,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result> ExecuteAsync(
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        LogDeletingWorkspace(logger, id);

        var workspace = await workspaceRepository.GetByIdAsync(id, cancellationToken);
        if (workspace is null)
        {
            LogWorkspaceDeletionFailed(logger, id, "Not found");
            return Result.NotFound(UserErrorMessages.WorkspaceNotFound);
        }

        var canDelete = await authorizationGateway.CanWriteOrganizationAsync(user, workspace.OrganizationId, cancellationToken);
        if (!canDelete)
        {
            LogWorkspaceDeletionFailed(logger, id, "Forbidden");
            return Result.Forbidden(UserErrorMessages.WorkspaceDeleteForbidden);
        }

        if (await workspaceRepository.HasGoalsAsync(id, cancellationToken))
        {
            LogWorkspaceDeletionFailed(logger, id, "Has goals");
            return Result.Failure(
                "Não é possível excluir o workspace porque existem metas associadas a ele.",
                ErrorType.Conflict);
        }

        await workspaceRepository.RemoveAsync(workspace, cancellationToken);
        await unitOfWork.CommitAsync(workspaceRepository.SaveChangesAsync, cancellationToken);

        LogWorkspaceDeleted(logger, id);
        return Result.Success();
    }

    [LoggerMessage(EventId = 4026, Level = LogLevel.Information, Message = "Deleting workspace {WorkspaceId}")]
    private static partial void LogDeletingWorkspace(ILogger logger, Guid workspaceId);

    [LoggerMessage(EventId = 4027, Level = LogLevel.Information, Message = "Workspace deleted successfully: {WorkspaceId}")]
    private static partial void LogWorkspaceDeleted(ILogger logger, Guid workspaceId);

    [LoggerMessage(EventId = 4028, Level = LogLevel.Warning, Message = "Workspace deletion failed for {WorkspaceId}: {Reason}")]
    private static partial void LogWorkspaceDeletionFailed(ILogger logger, Guid workspaceId, string reason);
}
