using Bud.Server.Infrastructure.Repositories;
using Bud.Server.Application.Common;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Application.Organizations;

public sealed class OrganizationQueryUseCase(IOrganizationRepository organizationRepository) : IOrganizationQueryUseCase
{
    public async Task<Result<Organization>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var organization = await organizationRepository.GetByIdAsync(id, cancellationToken);
        return organization is null
            ? Result<Organization>.NotFound("Organização não encontrada.")
            : Result<Organization>.Success(organization);
    }

    public async Task<Result<PagedResult<Organization>>> GetAllAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);
        var result = await organizationRepository.GetAllAsync(search, page, pageSize, cancellationToken);
        return Result<PagedResult<Organization>>.Success(result);
    }

    public async Task<Result<PagedResult<Workspace>>> GetWorkspacesAsync(
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

    public async Task<Result<PagedResult<Collaborator>>> GetCollaboratorsAsync(
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
