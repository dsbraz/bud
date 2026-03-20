using Bud.Application.Common;
using Bud.Shared.Contracts;
using Microsoft.Extensions.Logging;

namespace Bud.Application.Organizations;

public sealed partial class CreateOrganization(
    IOrganizationRepository organizationRepository,
    ILogger<CreateOrganization> logger,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<Organization>> ExecuteAsync(
        CreateOrganizationRequest request,
        CancellationToken cancellationToken = default)
    {
        LogCreatingOrganization(logger, request.Name);

        try
        {
            var organization = Organization.Create(Guid.NewGuid(), request.Name, request.Plan, request.IconUrl);

            await organizationRepository.AddAsync(organization, cancellationToken);
            await unitOfWork.CommitAsync(organizationRepository.SaveChangesAsync, cancellationToken);

            LogOrganizationCreated(logger, organization.Id, organization.Name);
            return Result<Organization>.Success(organization);
        }
        catch (DomainInvariantException ex)
        {
            LogOrganizationCreationFailed(logger, request.Name, ex.Message);
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
