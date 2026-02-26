using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Server.Settings;
using Bud.Shared.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bud.Server.Application.UseCases.Organizations;

public sealed partial class CreateOrganization(
    IOrganizationRepository organizationRepository,
    ICollaboratorRepository collaboratorRepository,
    ILogger<CreateOrganization> logger,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<Organization>> ExecuteAsync(
        CreateOrganizationRequest request,
        CancellationToken cancellationToken = default)
    {
        LogCreatingOrganization(logger, request.Name);

        var owner = await collaboratorRepository.GetByIdAsync(request.OwnerId, cancellationToken);
        if (owner is null)
        {
            LogOrganizationCreationFailed(logger, request.Name, "Owner not found");
            return Result<Organization>.NotFound("O líder selecionado não foi encontrado.");
        }

        if (owner.Role != CollaboratorRole.Leader)
        {
            LogOrganizationCreationFailed(logger, request.Name, "Owner is not a leader");
            return Result<Organization>.Failure(
                "O proprietário da organização deve ter a função de Líder.",
                ErrorType.Validation);
        }

        try
        {
            var organization = Organization.Create(Guid.NewGuid(), request.Name, request.OwnerId);

            await organizationRepository.AddAsync(organization, cancellationToken);
            await unitOfWork.CommitAsync(organizationRepository.SaveChangesAsync, cancellationToken);

            organization.Owner = owner;
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
