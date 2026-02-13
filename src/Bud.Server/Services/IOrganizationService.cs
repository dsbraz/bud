using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Services;

public interface IOrganizationService
{
    Task<ServiceResult<Organization>> CreateAsync(CreateOrganizationRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<Organization>> UpdateAsync(Guid id, UpdateOrganizationRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ServiceResult<Organization>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ServiceResult<PagedResult<Organization>>> GetAllAsync(string? search, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ServiceResult<PagedResult<Workspace>>> GetWorkspacesAsync(Guid id, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ServiceResult<PagedResult<Collaborator>>> GetCollaboratorsAsync(Guid id, int page, int pageSize, CancellationToken cancellationToken = default);
}
