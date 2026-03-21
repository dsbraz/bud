using Bud.Application.Common;
using Microsoft.Extensions.Logging;

namespace Bud.Application.Features.Organizations.UseCases;

public sealed record CreateOrganizationCommand(string Name, Guid OwnerId);

public sealed partial class CreateOrganization(
    IOrganizationRepository organizationRepository,
    ICollaboratorRepository collaboratorRepository,
    ILogger<CreateOrganization> logger,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<Organization>> ExecuteAsync(
        CreateOrganizationCommand command,
        CancellationToken cancellationToken = default)
    {
        LogCreatingOrganization(logger, command.Name);

        var owner = await collaboratorRepository.GetByIdAsync(command.OwnerId, cancellationToken);
        if (owner is null)
        {
            LogOrganizationCreationFailed(logger, command.Name, UserErrorMessages.SelectedOwnerNotFound);
            return Result<Organization>.NotFound(UserErrorMessages.SelectedOwnerNotFound);
        }

        try
        {
            owner.EnsureCanOwnOrganization();
        }
        catch (DomainInvariantException ex)
        {
            LogOrganizationCreationFailed(logger, command.Name, ex.Message);
            return Result<Organization>.Failure(ex.Message, ErrorType.Validation);
        }

        try
        {
            var organization = Organization.Create(Guid.NewGuid(), command.Name, command.OwnerId);

            await organizationRepository.AddAsync(organization, cancellationToken);
            await unitOfWork.CommitAsync(organizationRepository.SaveChangesAsync, cancellationToken);

            organization.Owner = owner;
            LogOrganizationCreated(logger, organization.Id, organization.Name);
            return Result<Organization>.Success(organization);
        }
        catch (DomainInvariantException ex)
        {
            LogOrganizationCreationFailed(logger, command.Name, ex.Message);
            return Result<Organization>.Failure(ex.Message, ErrorType.Validation);
        }
    }

    [LoggerMessage(EventId = 4010, Level = LogLevel.Information, Message = "Creating organization '{Name}'")]
    private static partial void LogCreatingOrganization(ILogger logger, string name);

    [LoggerMessage(EventId = 4011, Level = LogLevel.Information, Message = "Organization created successfully: {OrganizationId} - '{Name}'")]
    private static partial void LogOrganizationCreated(ILogger logger, Guid organizationId, string name);

    [LoggerMessage(EventId = 4012, Level = LogLevel.Warning, Message = "Organization creation failed for '{Name}': {Reason}")]
    private static partial void LogOrganizationCreationFailed(ILogger logger, string name, string reason);
}
