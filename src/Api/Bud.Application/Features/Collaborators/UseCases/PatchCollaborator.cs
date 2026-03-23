using System.Security.Claims;
using Bud.Application.Common;
using Bud.Application.Ports;
using Microsoft.Extensions.Logging;

namespace Bud.Application.Features.Collaborators.UseCases;

public sealed record PatchCollaboratorCommand(
    Optional<string> FullName,
    Optional<string> Email,
    Optional<CollaboratorRole> Role,
    Optional<Guid?> LeaderId);

public sealed partial class PatchCollaborator(
    ICollaboratorRepository collaboratorRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    ILogger<PatchCollaborator> logger,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<Collaborator>> ExecuteAsync(
        ClaimsPrincipal user,
        Guid id,
        PatchCollaboratorCommand command,
        CancellationToken cancellationToken = default)
    {
        LogPatchingCollaborator(logger, id);

        var collaborator = await collaboratorRepository.GetByIdAsync(id, cancellationToken);
        if (collaborator is null)
        {
            LogCollaboratorPatchFailed(logger, id, "Not found");
            return Result<Collaborator>.NotFound(UserErrorMessages.CollaboratorNotFound);
        }

        var canUpdate = await authorizationGateway.IsOrganizationOwnerAsync(user, collaborator.OrganizationId, cancellationToken);
        if (!canUpdate)
        {
            LogCollaboratorPatchFailed(logger, id, "Forbidden");
            return Result<Collaborator>.Forbidden(UserErrorMessages.CollaboratorUpdateForbidden);
        }

        var requestedEmail = command.Email.HasValue ? command.Email.Value : collaborator.Email;
        var requestedFullName = command.FullName.HasValue ? command.FullName.Value : collaborator.FullName;
        var requestedLeaderId = command.LeaderId.HasValue ? command.LeaderId.Value : collaborator.LeaderId;
        var requestedRole = command.Role.HasValue ? command.Role.Value : collaborator.Role;

        if (!EmailAddress.TryCreate(requestedEmail, out var emailAddress))
        {
            LogCollaboratorPatchFailed(logger, id, "Invalid email");
            return Result<Collaborator>.Failure(UserErrorMessages.CollaboratorInvalidEmail, ErrorType.Validation);
        }

        if (!PersonName.TryCreate(requestedFullName, out var personName))
        {
            LogCollaboratorPatchFailed(logger, id, "Invalid name");
            return Result<Collaborator>.Failure(UserErrorMessages.CollaboratorNameRequired, ErrorType.Validation);
        }

        if (collaborator.Email != emailAddress.Value)
        {
            if (!await collaboratorRepository.IsEmailUniqueAsync(emailAddress.Value, id, cancellationToken))
            {
                LogCollaboratorPatchFailed(logger, id, "Email already in use");
                return Result<Collaborator>.Failure(UserErrorMessages.CollaboratorEmailAlreadyInUse, ErrorType.Validation);
            }
        }

        if (command.LeaderId.HasValue && requestedLeaderId.HasValue)
        {
            var leader = await collaboratorRepository.GetByIdAsync(requestedLeaderId.Value, cancellationToken);
            if (leader is null)
            {
                LogCollaboratorPatchFailed(logger, id, "Leader not found");
                return Result<Collaborator>.NotFound(UserErrorMessages.LeaderNotFound);
            }

            try
            {
                leader.EnsureCanLeadOrganization(collaborator.OrganizationId);
            }
            catch (DomainInvariantException ex)
            {
                LogCollaboratorPatchFailed(logger, id, ex.Message);
                return Result<Collaborator>.Failure(ex.Message, ErrorType.Validation);
            }
        }

        if (collaborator.Role == CollaboratorRole.Leader &&
            requestedRole == CollaboratorRole.IndividualContributor)
        {
            if (await collaboratorRepository.HasSubordinatesAsync(id, cancellationToken))
            {
                LogCollaboratorPatchFailed(logger, id, "Leader has subordinates");
                return Result<Collaborator>.Failure(
                    "Não é possível alterar o perfil. Este líder possui membros de equipe.",
                    ErrorType.Validation);
            }

            if (await collaboratorRepository.IsOrganizationOwnerAsync(id, cancellationToken))
            {
                LogCollaboratorPatchFailed(logger, id, "Leader is organization owner");
                return Result<Collaborator>.Failure(
                    "Não é possível alterar o perfil. Este líder é proprietário de uma organização.",
                    ErrorType.Validation);
            }
        }

        try
        {
            collaborator.UpdateProfile(
                personName.Value,
                emailAddress.Value,
                requestedRole,
                requestedLeaderId,
                collaborator.Id);
            await unitOfWork.CommitAsync(collaboratorRepository.SaveChangesAsync, cancellationToken);

            LogCollaboratorPatched(logger, id, collaborator.FullName);
            return Result<Collaborator>.Success(collaborator);
        }
        catch (DomainInvariantException ex)
        {
            LogCollaboratorPatchFailed(logger, id, ex.Message);
            return Result<Collaborator>.Failure(ex.Message, ErrorType.Validation);
        }
    }

    [LoggerMessage(EventId = 4043, Level = LogLevel.Information, Message = "Patching collaborator {CollaboratorId}")]
    private static partial void LogPatchingCollaborator(ILogger logger, Guid collaboratorId);

    [LoggerMessage(EventId = 4044, Level = LogLevel.Information, Message = "Collaborator patched successfully: {CollaboratorId} - '{FullName}'")]
    private static partial void LogCollaboratorPatched(ILogger logger, Guid collaboratorId, string fullName);

    [LoggerMessage(EventId = 4045, Level = LogLevel.Warning, Message = "Collaborator patch failed for {CollaboratorId}: {Reason}")]
    private static partial void LogCollaboratorPatchFailed(ILogger logger, Guid collaboratorId, string reason);
}
