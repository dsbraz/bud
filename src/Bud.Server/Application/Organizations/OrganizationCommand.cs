using Bud.Server.Infrastructure.Repositories;
using Bud.Server.Application.Common;
using Bud.Server.Settings;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using Microsoft.Extensions.Options;

namespace Bud.Server.Application.Organizations;

public sealed class OrganizationCommand(
    IOrganizationRepository organizationRepository,
    ICollaboratorRepository collaboratorRepository,
    IOptions<GlobalAdminSettings> globalAdminSettings)
{
    private readonly string _globalAdminOrgName = globalAdminSettings.Value.OrganizationName;

    public async Task<Result<Organization>> CreateAsync(CreateOrganizationRequest request, CancellationToken cancellationToken = default)
    {
        var owner = await collaboratorRepository.GetByIdAsync(request.OwnerId, cancellationToken);
        if (owner is null)
        {
            return Result<Organization>.NotFound("O líder selecionado não foi encontrado.");
        }

        if (owner.Role != CollaboratorRole.Leader)
        {
            return Result<Organization>.Failure(
                "O proprietário da organização deve ter a função de Líder.",
                ErrorType.Validation);
        }

        try
        {
            var organization = Organization.Create(Guid.NewGuid(), request.Name, request.OwnerId);

            await organizationRepository.AddAsync(organization, cancellationToken);
            await organizationRepository.SaveChangesAsync(cancellationToken);

            var result = await organizationRepository.GetByIdWithOwnerAsync(organization.Id, cancellationToken);
            return Result<Organization>.Success(result!);
        }
        catch (DomainInvariantException ex)
        {
            return Result<Organization>.Failure(ex.Message, ErrorType.Validation);
        }
    }

    public async Task<Result<Organization>> UpdateAsync(Guid id, UpdateOrganizationRequest request, CancellationToken cancellationToken = default)
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
            organization.Rename(request.Name);

            if (request.OwnerId.HasValue && request.OwnerId.Value != Guid.Empty)
            {
                var newOwner = await collaboratorRepository.GetByIdAsync(request.OwnerId.Value, cancellationToken);
                if (newOwner is null)
                {
                    return Result<Organization>.NotFound("O líder selecionado não foi encontrado.");
                }

                if (newOwner.Role != CollaboratorRole.Leader)
                {
                    return Result<Organization>.Failure(
                        "O proprietário da organização deve ter a função de Líder.",
                        ErrorType.Validation);
                }

                organization.AssignOwner(request.OwnerId.Value);
            }

            await organizationRepository.SaveChangesAsync(cancellationToken);

            return Result<Organization>.Success(organization);
        }
        catch (DomainInvariantException ex)
        {
            return Result<Organization>.Failure(ex.Message, ErrorType.Validation);
        }
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
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
        await organizationRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private bool IsProtectedOrganization(string organizationName)
        => !string.IsNullOrEmpty(_globalAdminOrgName) &&
           organizationName.Equals(_globalAdminOrgName, StringComparison.OrdinalIgnoreCase);
}
