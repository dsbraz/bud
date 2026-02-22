using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Domain.Abstractions;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Server.Settings;
using Bud.Shared.Contracts;
using Microsoft.Extensions.Options;

namespace Bud.Server.Application.UseCases.Organizations;

public sealed class DeleteOrganization(
    IOrganizationRepository organizationRepository,
    IOptions<GlobalAdminSettings> globalAdminSettings,
    IUnitOfWork? unitOfWork = null)
{
    private readonly string _globalAdminOrgName = globalAdminSettings.Value.OrganizationName;

    public async Task<Result> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var organization = await organizationRepository.GetByIdWithOwnerAsync(id, cancellationToken);
        if (organization is null)
        {
            return Result.NotFound("Organização não encontrada.");
        }

        if (IsProtectedOrganization(organization.Name))
        {
            return Result.Failure(
                "Esta organização está protegida e não pode ser excluída.",
                ErrorType.Validation);
        }

        if (await organizationRepository.HasWorkspacesAsync(id, cancellationToken))
        {
            return Result.Failure(
                "Não é possível excluir a organização porque ela possui workspaces associados. Exclua os workspaces primeiro.",
                ErrorType.Conflict);
        }

        if (await organizationRepository.HasCollaboratorsAsync(id, cancellationToken))
        {
            return Result.Failure(
                "Não é possível excluir a organização porque ela possui colaboradores associados. Remova os colaboradores primeiro.",
                ErrorType.Conflict);
        }

        await organizationRepository.RemoveAsync(organization, cancellationToken);
        await unitOfWork.CommitAsync(organizationRepository.SaveChangesAsync, cancellationToken);

        return Result.Success();
    }

    private bool IsProtectedOrganization(string organizationName)
        => !string.IsNullOrEmpty(_globalAdminOrgName) &&
           organizationName.Equals(_globalAdminOrgName, StringComparison.OrdinalIgnoreCase);
}

