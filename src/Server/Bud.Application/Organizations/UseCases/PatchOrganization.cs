using Bud.Application.Common;
using Bud.Application.Configuration;
using Bud.Shared.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bud.Application.Organizations;

public sealed partial class PatchOrganization(
    IOrganizationRepository organizationRepository,
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

        var organization = await organizationRepository.GetByIdAsync(id, cancellationToken);
        if (organization is null)
        {
            LogOrganizationPatchFailed(logger, id, "Not found");
            return Result<Organization>.NotFound(UserErrorMessages.OrganizationNotFound);
        }

        if (OrganizationProtectionPolicy.IsProtectedOrganization(organization.Name, _globalAdminOrgName))
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

            if (request.Plan.HasValue)
            {
                organization.SetPlan(request.Plan.Value);
            }

            if (request.IconUrl.HasValue)
            {
                organization.SetIconUrl(request.IconUrl.Value);
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

    [LoggerMessage(EventId = 4013, Level = LogLevel.Information, Message = "Patching organization {OrganizationId}")]
    private static partial void LogPatchingOrganization(ILogger logger, Guid organizationId);

    [LoggerMessage(EventId = 4014, Level = LogLevel.Information, Message = "Organization patched successfully: {OrganizationId} - '{Name}'")]
    private static partial void LogOrganizationPatched(ILogger logger, Guid organizationId, string name);

    [LoggerMessage(EventId = 4015, Level = LogLevel.Warning, Message = "Organization patch failed for {OrganizationId}: {Reason}")]
    private static partial void LogOrganizationPatchFailed(ILogger logger, Guid organizationId, string reason);
}
