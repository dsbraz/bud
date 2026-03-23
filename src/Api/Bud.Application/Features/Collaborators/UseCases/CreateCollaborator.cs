using System.Security.Claims;
using Bud.Application.Common;
using Bud.Application.Ports;
using Microsoft.Extensions.Logging;

namespace Bud.Application.Features.Collaborators.UseCases;

public sealed record CreateCollaboratorCommand(
    string FullName,
    string Email,
    CollaboratorRole Role,
    Guid? TeamId,
    Guid? LeaderId);

public sealed partial class CreateCollaborator(
    ICollaboratorRepository collaboratorRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    ITenantProvider tenantProvider,
    ILogger<CreateCollaborator> logger,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<Collaborator>> ExecuteAsync(
        ClaimsPrincipal user,
        CreateCollaboratorCommand command,
        CancellationToken cancellationToken = default)
    {
        LogCreatingCollaborator(logger, command.FullName);

        var organizationId = tenantProvider.TenantId;
        if (!organizationId.HasValue)
        {
            LogCollaboratorCreationFailed(logger, command.FullName, "Organization context not found");
            return Result<Collaborator>.Failure(UserErrorMessages.CollaboratorContextNotFound, ErrorType.Validation);
        }

        var canCreate = await authorizationGateway.IsOrganizationOwnerAsync(user, organizationId.Value, cancellationToken);
        if (!canCreate)
        {
            LogCollaboratorCreationFailed(logger, command.FullName, "Forbidden");
            return Result<Collaborator>.Forbidden(UserErrorMessages.CollaboratorCreateForbidden);
        }

        if (!EmailAddress.TryCreate(command.Email, out var emailAddress))
        {
            LogCollaboratorCreationFailed(logger, command.FullName, "Invalid email");
            return Result<Collaborator>.Failure(UserErrorMessages.CollaboratorInvalidEmail, ErrorType.Validation);
        }

        if (!PersonName.TryCreate(command.FullName, out var personName))
        {
            LogCollaboratorCreationFailed(logger, command.FullName, "Invalid name");
            return Result<Collaborator>.Failure(UserErrorMessages.CollaboratorNameRequired, ErrorType.Validation);
        }

        try
        {
            var collaborator = Collaborator.Create(
                Guid.NewGuid(),
                organizationId.Value,
                personName.Value,
                emailAddress.Value,
                command.Role,
                command.LeaderId);

            await collaboratorRepository.AddAsync(collaborator, cancellationToken);
            await unitOfWork.CommitAsync(collaboratorRepository.SaveChangesAsync, cancellationToken);

            LogCollaboratorCreated(logger, collaborator.Id, collaborator.FullName);
            return Result<Collaborator>.Success(collaborator);
        }
        catch (DomainInvariantException ex)
        {
            LogCollaboratorCreationFailed(logger, command.FullName, ex.Message);
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
