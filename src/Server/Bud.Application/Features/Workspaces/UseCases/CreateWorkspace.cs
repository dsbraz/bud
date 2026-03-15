using System.Security.Claims;
using Bud.Application.Common;
using Bud.Application.Ports;
using Bud.Shared.Contracts;
using Microsoft.Extensions.Logging;

namespace Bud.Application.Features.Workspaces;

public sealed partial class CreateWorkspace(
    IWorkspaceRepository workspaceRepository,
    IOrganizationRepository organizationRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    ILogger<CreateWorkspace> logger,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<Workspace>> ExecuteAsync(
        ClaimsPrincipal user,
        CreateWorkspaceRequest request,
        CancellationToken cancellationToken = default)
    {
        LogCreatingWorkspace(logger, request.Name, request.OrganizationId);

        var canCreate = await authorizationGateway.IsOrganizationOwnerAsync(user, request.OrganizationId, cancellationToken);
        if (!canCreate)
        {
            LogWorkspaceCreationFailed(logger, request.Name, "Forbidden");
            return Result<Workspace>.Forbidden(UserErrorMessages.WorkspaceCreateForbidden);
        }

        if (!await organizationRepository.ExistsAsync(request.OrganizationId, cancellationToken))
        {
            LogWorkspaceCreationFailed(logger, request.Name, "Organization not found");
            return Result<Workspace>.NotFound(UserErrorMessages.OrganizationNotFound);
        }

        if (!await workspaceRepository.IsNameUniqueAsync(request.OrganizationId, request.Name, ct: cancellationToken))
        {
            LogWorkspaceCreationFailed(logger, request.Name, "Name already exists");
            return Result<Workspace>.Failure(UserErrorMessages.WorkspaceNameConflict, ErrorType.Conflict);
        }

        try
        {
            var workspace = Workspace.Create(Guid.NewGuid(), request.OrganizationId, request.Name);

            await workspaceRepository.AddAsync(workspace, cancellationToken);
            await unitOfWork.CommitAsync(workspaceRepository.SaveChangesAsync, cancellationToken);

            LogWorkspaceCreated(logger, workspace.Id, workspace.Name);
            return Result<Workspace>.Success(workspace);
        }
        catch (DomainInvariantException ex)
        {
            LogWorkspaceCreationFailed(logger, request.Name, ex.Message);
            return Result<Workspace>.Failure(ex.Message, ErrorType.Validation);
        }
    }

    [LoggerMessage(EventId = 4020, Level = LogLevel.Information, Message = "Creating workspace '{Name}' for organization {OrganizationId}")]
    private static partial void LogCreatingWorkspace(ILogger logger, string name, Guid organizationId);

    [LoggerMessage(EventId = 4021, Level = LogLevel.Information, Message = "Workspace created successfully: {WorkspaceId} - '{Name}'")]
    private static partial void LogWorkspaceCreated(ILogger logger, Guid workspaceId, string name);

    [LoggerMessage(EventId = 4022, Level = LogLevel.Warning, Message = "Workspace creation failed for '{Name}': {Reason}")]
    private static partial void LogWorkspaceCreationFailed(ILogger logger, string name, string reason);
}
