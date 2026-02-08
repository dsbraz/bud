using Bud.Shared.Contracts;
using Bud.Shared.Models;

namespace Bud.Server.Application.Organizations;

public interface IOrganizationQueryUseCase
{
    Task<ServiceResult<Organization>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ServiceResult<PagedResult<Organization>>> GetAllAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<PagedResult<Workspace>>> GetWorkspacesAsync(
        Guid id,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<PagedResult<Collaborator>>> GetCollaboratorsAsync(
        Guid id,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
