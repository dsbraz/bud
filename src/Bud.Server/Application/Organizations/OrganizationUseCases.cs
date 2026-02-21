using Bud.Server.Application.Common;
using Bud.Server.Domain.Abstractions;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Server.Settings;
using Bud.Shared.Contracts;
using Microsoft.Extensions.Options;

namespace Bud.Server.Application.Organizations;

public sealed class RegisterOrganization(
    IOrganizationRepository organizationRepository,
    ICollaboratorRepository collaboratorRepository,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<Organization>> ExecuteAsync(
        CreateOrganizationRequest request,
        CancellationToken cancellationToken = default)
    {
        var owner = await collaboratorRepository.GetByIdAsync(request.OwnerId, cancellationToken);
        if (owner is null)
        {
            return Result<Organization>.NotFound("O líder selecionado não foi encontrado.");
        }

        if (owner.Role != Bud.Server.Domain.Model.CollaboratorRole.Leader)
        {
            return Result<Organization>.Failure(
                "O proprietário da organização deve ter a função de Líder.",
                ErrorType.Validation);
        }

        try
        {
            var organization = Organization.Create(Guid.NewGuid(), request.Name, request.OwnerId);

            await organizationRepository.AddAsync(organization, cancellationToken);
            await unitOfWork.CommitAsync(organizationRepository.SaveChangesAsync, cancellationToken);

            var result = await organizationRepository.GetByIdWithOwnerAsync(organization.Id, cancellationToken);
            return Result<Organization>.Success(result!);
        }
        catch (DomainInvariantException ex)
        {
            return Result<Organization>.Failure(ex.Message, ErrorType.Validation);
        }
    }
}

public sealed class RenameOrganization(
    IOrganizationRepository organizationRepository,
    ICollaboratorRepository collaboratorRepository,
    IOptions<GlobalAdminSettings> globalAdminSettings,
    IUnitOfWork? unitOfWork = null)
{
    private readonly string _globalAdminOrgName = globalAdminSettings.Value.OrganizationName;

    public async Task<Result<Organization>> ExecuteAsync(
        Guid id,
        UpdateOrganizationRequest request,
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
            organization.Rename(request.Name);

            if (request.OwnerId.HasValue && request.OwnerId.Value != Guid.Empty)
            {
                var newOwner = await collaboratorRepository.GetByIdAsync(request.OwnerId.Value, cancellationToken);
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

                organization.AssignOwner(request.OwnerId.Value);
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

public sealed class ViewOrganizationDetails(IOrganizationRepository organizationRepository)
{
    public async Task<Result<Organization>> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var organization = await organizationRepository.GetByIdAsync(id, cancellationToken);
        return organization is null
            ? Result<Organization>.NotFound("Organização não encontrada.")
            : Result<Organization>.Success(organization);
    }
}

public sealed class ListOrganizations(IOrganizationRepository organizationRepository)
{
    public async Task<Result<PagedResult<Organization>>> ExecuteAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);
        var result = await organizationRepository.GetAllAsync(search, page, pageSize, cancellationToken);
        return Result<PagedResult<Organization>>.Success(result);
    }
}

public sealed class ListOrganizationWorkspaces(IOrganizationRepository organizationRepository)
{
    public async Task<Result<PagedResult<Workspace>>> ExecuteAsync(
        Guid id,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);

        if (!await organizationRepository.ExistsAsync(id, cancellationToken))
        {
            return Result<PagedResult<Workspace>>.NotFound("Organização não encontrada.");
        }

        var result = await organizationRepository.GetWorkspacesAsync(id, page, pageSize, cancellationToken);
        return Result<PagedResult<Workspace>>.Success(result);
    }
}

public sealed class ListOrganizationCollaborators(IOrganizationRepository organizationRepository)
{
    public async Task<Result<PagedResult<Collaborator>>> ExecuteAsync(
        Guid id,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);

        if (!await organizationRepository.ExistsAsync(id, cancellationToken))
        {
            return Result<PagedResult<Collaborator>>.NotFound("Organização não encontrada.");
        }

        var result = await organizationRepository.GetCollaboratorsAsync(id, page, pageSize, cancellationToken);
        return Result<PagedResult<Collaborator>>.Success(result);
    }
}
