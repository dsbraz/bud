using System.Security.Claims;
using Bud.Application.Common;
using Bud.Application.Ports;
using Microsoft.Extensions.Logging;

namespace Bud.Application.Features.Workspaces.UseCases;

public sealed record CreateWorkspaceCommand(string Name, Guid OrganizationId);

public sealed partial class CreateWorkspace(
    IWorkspaceRepository workspaceRepository,
    IOrganizationRepository organizationRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    ILogger<CreateWorkspace> logger,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<Workspace>> ExecuteAsync(
        ClaimsPrincipal user,
        CreateWorkspaceCommand command,
        CancellationToken cancellationToken = default)
    {
        LogCreatingWorkspace(logger, command.Name, command.OrganizationId);

        var canCreate = await authorizationGateway.IsOrganizationOwnerAsync(user, command.OrganizationId, cancellationToken);
        if (!canCreate)
        {
            LogWorkspaceCreationFailed(logger, command.Name, "Forbidden");
            return Result<Workspace>.Forbidden(UserErrorMessages.WorkspaceCreateForbidden);
        }

        if (!await organizationRepository.ExistsAsync(command.OrganizationId, cancellationToken))
        {
            LogWorkspaceCreationFailed(logger, command.Name, "Organization not found");
            return Result<Workspace>.NotFound(UserErrorMessages.OrganizationNotFound);
        }

        if (!await workspaceRepository.IsNameUniqueAsync(command.OrganizationId, command.Name, ct: cancellationToken))
        {
            LogWorkspaceCreationFailed(logger, command.Name, "Name already exists");
            return Result<Workspace>.Failure(UserErrorMessages.WorkspaceNameConflict, ErrorType.Conflict);
        }

        try
        {
            var workspace = Workspace.Create(Guid.NewGuid(), command.OrganizationId, command.Name);

            await workspaceRepository.AddAsync(workspace, cancellationToken);
            await unitOfWork.CommitAsync(workspaceRepository.SaveChangesAsync, cancellationToken);

            LogWorkspaceCreated(logger, workspace.Id, workspace.Name);
            return Result<Workspace>.Success(workspace);
        }
        catch (DomainInvariantException ex)
        {
            LogWorkspaceCreationFailed(logger, command.Name, ex.Message);
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
