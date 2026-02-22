using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Domain.Abstractions;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Server.Settings;
using Bud.Shared.Contracts;
using Microsoft.Extensions.Options;

namespace Bud.Server.Application.UseCases.Organizations;

public sealed class PatchOrganization(
    IOrganizationRepository organizationRepository,
    ICollaboratorRepository collaboratorRepository,
    IOptions<GlobalAdminSettings> globalAdminSettings,
    IUnitOfWork? unitOfWork = null)
{
    private readonly string _globalAdminOrgName = globalAdminSettings.Value.OrganizationName;

    public async Task<Result<Organization>> ExecuteAsync(
        Guid id,
        PatchOrganizationRequest request,
        CancellationToken cancellationToken = default)
    {
        var organization = await organizationRepository.GetByIdWithOwnerAsync(id, cancellationToken);
        if (organization is null)
        {
            return Result<Organization>.NotFound("Organização não encontrada.");
        }

        if (IsProtectedOrganization(organization.Name))
        {
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
                    return Result<Organization>.NotFound("O líder selecionado não foi encontrado.");
                }

                if (newOwner.Role != Bud.Server.Domain.Model.CollaboratorRole.Leader)
                {
                    return Result<Organization>.Failure(
                        "O proprietário da organização deve ter a função de Líder.",
                        ErrorType.Validation);
                }

                organization.AssignOwner(ownerId);
            }

            await unitOfWork.CommitAsync(organizationRepository.SaveChangesAsync, cancellationToken);

            return Result<Organization>.Success(organization);
        }
        catch (DomainInvariantException ex)
        {
            return Result<Organization>.Failure(ex.Message, ErrorType.Validation);
        }
    }

    private bool IsProtectedOrganization(string organizationName)
        => !string.IsNullOrEmpty(_globalAdminOrgName) &&
           organizationName.Equals(_globalAdminOrgName, StringComparison.OrdinalIgnoreCase);
}
