using Bud.Shared.Contracts;
using Bud.Shared.Models;

namespace Bud.Server.Application.Organizations;

public sealed class OrganizationQueryUseCase(IOrganizationService organizationService) : IOrganizationQueryUseCase
{
    public Task<ServiceResult<Organization>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => organizationService.GetByIdAsync(id, cancellationToken);

    public Task<ServiceResult<PagedResult<Organization>>> GetAllAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
        => organizationService.GetAllAsync(search, page, pageSize, cancellationToken);

    public Task<ServiceResult<PagedResult<Workspace>>> GetWorkspacesAsync(
        Guid id,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
        => organizationService.GetWorkspacesAsync(id, page, pageSize, cancellationToken);

    public Task<ServiceResult<PagedResult<Collaborator>>> GetCollaboratorsAsync(
        Guid id,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
        => organizationService.GetCollaboratorsAsync(id, page, pageSize, cancellationToken);
}
