using Bud.Server.Application.Common;
using Bud.Server.Application.Policies;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Server.Settings;
using Bud.Shared.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bud.Server.Application.UseCases.Organizations;

public sealed partial class DeleteOrganization(
    IOrganizationRepository organizationRepository,
    IOptions<GlobalAdminSettings> globalAdminSettings,
    ILogger<DeleteOrganization> logger,
    IUnitOfWork? unitOfWork = null)
{
    private readonly string _globalAdminOrgName = globalAdminSettings.Value.OrganizationName;

    public async Task<Result> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        LogDeletingOrganization(logger, id);

        var organization = await organizationRepository.GetByIdWithOwnerAsync(id, cancellationToken);
        if (organization is null)
        {
            LogOrganizationDeletionFailed(logger, id, "Not found");
            return Result.NotFound(UserErrorMessages.OrganizationNotFound);
        }

        if (OrganizationProtectionPolicy.IsProtectedOrganization(organization.Name, _globalAdminOrgName))
        {
            LogOrganizationDeletionFailed(logger, id, "Protected organization");
            return Result.Failure(
                "Esta organização está protegida e não pode ser excluída.",
                ErrorType.Validation);
        }

        if (await organizationRepository.HasWorkspacesAsync(id, cancellationToken))
        {
            LogOrganizationDeletionFailed(logger, id, "Has workspaces");
            return Result.Failure(
                "Não é possível excluir a organização porque ela possui workspaces associados. Exclua os workspaces primeiro.",
                ErrorType.Conflict);
        }

        if (await organizationRepository.HasCollaboratorsAsync(id, cancellationToken))
        {
            LogOrganizationDeletionFailed(logger, id, "Has collaborators");
            return Result.Failure(
                "Não é possível excluir a organização porque ela possui colaboradores associados. Remova os colaboradores primeiro.",
                ErrorType.Conflict);
        }

        await organizationRepository.RemoveAsync(organization, cancellationToken);
        await unitOfWork.CommitAsync(organizationRepository.SaveChangesAsync, cancellationToken);

        LogOrganizationDeleted(logger, id);
        return Result.Success();
    }

    [LoggerMessage(EventId = 4016, Level = LogLevel.Information, Message = "Deleting organization {OrganizationId}")]
    private static partial void LogDeletingOrganization(ILogger logger, Guid organizationId);

    [LoggerMessage(EventId = 4017, Level = LogLevel.Information, Message = "Organization deleted successfully: {OrganizationId}")]
    private static partial void LogOrganizationDeleted(ILogger logger, Guid organizationId);

    [LoggerMessage(EventId = 4018, Level = LogLevel.Warning, Message = "Organization deletion failed for {OrganizationId}: {Reason}")]
    private static partial void LogOrganizationDeletionFailed(ILogger logger, Guid organizationId, string reason);
}
