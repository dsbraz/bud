using System.Security.Claims;
using Bud.Application.Common;
using Bud.Application.Ports;
using Bud.Shared.Contracts;
using Microsoft.Extensions.Logging;

namespace Bud.Application.Workspaces;

public sealed partial class PatchWorkspace(
    IWorkspaceRepository workspaceRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    ILogger<PatchWorkspace> logger,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<Workspace>> ExecuteAsync(
        ClaimsPrincipal user,
        Guid id,
        PatchWorkspaceRequest request,
        CancellationToken cancellationToken = default)
    {
        LogPatchingWorkspace(logger, id);

        var workspace = await workspaceRepository.GetByIdAsync(id, cancellationToken);
        if (workspace is null)
        {
            LogWorkspacePatchFailed(logger, id, "Not found");
            return Result<Workspace>.NotFound(UserErrorMessages.WorkspaceNotFound);
        }

        var canUpdate = await authorizationGateway.CanWriteOrganizationAsync(user, workspace.OrganizationId, cancellationToken);
        if (!canUpdate)
        {
            LogWorkspacePatchFailed(logger, id, "Forbidden");
            return Result<Workspace>.Forbidden(UserErrorMessages.WorkspaceUpdateForbidden);
        }

        try
        {
            if (request.Name.HasValue)
            {
                var newName = request.Name.Value ?? string.Empty;

                if (!await workspaceRepository.IsNameUniqueAsync(workspace.OrganizationId, newName, excludeId: id, ct: cancellationToken))
                {
                    LogWorkspacePatchFailed(logger, id, "Name already exists");
                    return Result<Workspace>.Failure(UserErrorMessages.WorkspaceNameConflict, ErrorType.Conflict);
                }

                workspace.Rename(newName);
            }

            await unitOfWork.CommitAsync(workspaceRepository.SaveChangesAsync, cancellationToken);

            LogWorkspacePatched(logger, id, workspace.Name);
            return Result<Workspace>.Success(workspace);
        }
        catch (DomainInvariantException ex)
        {
            LogWorkspacePatchFailed(logger, id, ex.Message);
            return Result<Workspace>.Failure(ex.Message, ErrorType.Validation);
        }
    }

    [LoggerMessage(EventId = 4023, Level = LogLevel.Information, Message = "Patching workspace {WorkspaceId}")]
    private static partial void LogPatchingWorkspace(ILogger logger, Guid workspaceId);

    [LoggerMessage(EventId = 4024, Level = LogLevel.Information, Message = "Workspace patched successfully: {WorkspaceId} - '{Name}'")]
    private static partial void LogWorkspacePatched(ILogger logger, Guid workspaceId, string name);

    [LoggerMessage(EventId = 4025, Level = LogLevel.Warning, Message = "Workspace patch failed for {WorkspaceId}: {Reason}")]
    private static partial void LogWorkspacePatchFailed(ILogger logger, Guid workspaceId, string reason);
}
