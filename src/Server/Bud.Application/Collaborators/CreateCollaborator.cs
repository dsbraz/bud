using System.Security.Claims;
using Bud.Application.Common;
using Bud.Application.Ports;
using Bud.Shared.Contracts;
using Microsoft.Extensions.Logging;

namespace Bud.Application.Collaborators;

public sealed partial class CreateCollaborator(
    ICollaboratorRepository collaboratorRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    ITenantProvider tenantProvider,
    ILogger<CreateCollaborator> logger,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<Collaborator>> ExecuteAsync(
        ClaimsPrincipal user,
        CreateCollaboratorRequest request,
        CancellationToken cancellationToken = default)
    {
        LogCreatingCollaborator(logger, request.FullName);

        var organizationId = tenantProvider.TenantId;
        if (!organizationId.HasValue)
        {
            LogCollaboratorCreationFailed(logger, request.FullName, "Organization context not found");
            return Result<Collaborator>.Failure(UserErrorMessages.CollaboratorContextNotFound, ErrorType.Validation);
        }

        var canCreate = await authorizationGateway.IsOrganizationOwnerAsync(user, organizationId.Value, cancellationToken);
        if (!canCreate)
        {
            LogCollaboratorCreationFailed(logger, request.FullName, "Forbidden");
            return Result<Collaborator>.Forbidden(UserErrorMessages.CollaboratorCreateForbidden);
        }

        if (!EmailAddress.TryCreate(request.Email, out var emailAddress))
        {
            LogCollaboratorCreationFailed(logger, request.FullName, "Invalid email");
            return Result<Collaborator>.Failure(UserErrorMessages.CollaboratorInvalidEmail, ErrorType.Validation);
        }

        if (!PersonName.TryCreate(request.FullName, out var personName))
        {
            LogCollaboratorCreationFailed(logger, request.FullName, "Invalid name");
            return Result<Collaborator>.Failure(UserErrorMessages.CollaboratorNameRequired, ErrorType.Validation);
        }

        try
        {
            var requestedRole = request.Role;

            var collaborator = Collaborator.Create(
                Guid.NewGuid(),
                organizationId.Value,
                personName.Value,
                emailAddress.Value,
                requestedRole,
                request.LeaderId);

            await collaboratorRepository.AddAsync(collaborator, cancellationToken);
            await unitOfWork.CommitAsync(collaboratorRepository.SaveChangesAsync, cancellationToken);

            LogCollaboratorCreated(logger, collaborator.Id, collaborator.FullName);
            return Result<Collaborator>.Success(collaborator);
        }
        catch (DomainInvariantException ex)
        {
            LogCollaboratorCreationFailed(logger, request.FullName, ex.Message);
            return Result<Collaborator>.Failure(ex.Message, ErrorType.Validation);
        }
    }

    [LoggerMessage(EventId = 4040, Level = LogLevel.Information, Message = "Creating collaborator '{FullName}'")]
    private static partial void LogCreatingCollaborator(ILogger logger, string fullName);

    [LoggerMessage(EventId = 4041, Level = LogLevel.Information, Message = "Collaborator created successfully: {CollaboratorId} - '{FullName}'")]
    private static partial void LogCollaboratorCreated(ILogger logger, Guid collaboratorId, string fullName);

    [LoggerMessage(EventId = 4042, Level = LogLevel.Warning, Message = "Collaborator creation failed for '{FullName}': {Reason}")]
    private static partial void LogCollaboratorCreationFailed(ILogger logger, string fullName, string reason);
}
