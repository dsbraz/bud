using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Server.Settings;
using Bud.Shared.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bud.Server.Application.UseCases.Organizations;

public sealed partial class PatchOrganization(
    IOrganizationRepository organizationRepository,
    ICollaboratorRepository collaboratorRepository,
    IOptions<GlobalAdminSettings> globalAdminSettings,
    ILogger<PatchOrganization> logger,
    IUnitOfWork? unitOfWork = null)
{
    private readonly string _globalAdminOrgName = globalAdminSettings.Value.OrganizationName;

    public async Task<Result<Organization>> ExecuteAsync(
        Guid id,
        PatchOrganizationRequest request,
        CancellationToken cancellationToken = default)
    {
        LogPatchingOrganization(logger, id);

        var organization = await organizationRepository.GetByIdWithOwnerAsync(id, cancellationToken);
        if (organization is null)
        {
            LogOrganizationPatchFailed(logger, id, "Not found");
            return Result<Organization>.NotFound("Organização não encontrada.");
        }

        if (IsProtectedOrganization(organization.Name))
        {
            LogOrganizationPatchFailed(logger, id, "Protected organization");
            return Result<Organization>.Failure(
                "Esta organização está protegida e não pode ser alterada.",
                ErrorType.Validation);
        }

        try
        {
            if (request.Name.HasValue)
            {
                organization.Rename(request.Name.Value ?? string.Empty);
            }

            if (request.OwnerId.HasValue && request.OwnerId.Value.HasValue && request.OwnerId.Value.Value != Guid.Empty)
            {
                var ownerId = request.OwnerId.Value.Value;
                var newOwner = await collaboratorRepository.GetByIdAsync(ownerId, cancellationToken);
                if (newOwner is null)
                {
                    LogOrganizationPatchFailed(logger, id, "New owner not found");
                    return Result<Organization>.NotFound("O líder selecionado não foi encontrado.");
                }

                if (newOwner.Role != CollaboratorRole.Leader)
                {
                    LogOrganizationPatchFailed(logger, id, "New owner is not a leader");
                    return Result<Organization>.Failure(
                        "O proprietário da organização deve ter a função de Líder.",
                        ErrorType.Validation);
                }

                organization.AssignOwner(ownerId);
            }

            await unitOfWork.CommitAsync(organizationRepository.SaveChangesAsync, cancellationToken);

            LogOrganizationPatched(logger, id, organization.Name);
            return Result<Organization>.Success(organization);
        }
        catch (DomainInvariantException ex)
        {
            LogOrganizationPatchFailed(logger, id, ex.Message);
            return Result<Organization>.Failure(ex.Message, ErrorType.Validation);
        }
    }

    private bool IsProtectedOrganization(string organizationName)
        => !string.IsNullOrEmpty(_globalAdminOrgName) &&
           organizationName.Equals(_globalAdminOrgName, StringComparison.OrdinalIgnoreCase);

    [LoggerMessage(EventId = 4013, Level = LogLevel.Information, Message = "Patching organization {OrganizationId}")]
    private static partial void LogPatchingOrganization(ILogger logger, Guid organizationId);

    [LoggerMessage(EventId = 4014, Level = LogLevel.Information, Message = "Organization patched successfully: {OrganizationId} - '{Name}'")]
    private static partial void LogOrganizationPatched(ILogger logger, Guid organizationId, string name);

    [LoggerMessage(EventId = 4015, Level = LogLevel.Warning, Message = "Organization patch failed for {OrganizationId}: {Reason}")]
    private static partial void LogOrganizationPatchFailed(ILogger logger, Guid organizationId, string reason);
}
