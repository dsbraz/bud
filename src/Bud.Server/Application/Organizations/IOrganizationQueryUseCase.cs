using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using Bud.Server.Application.Common;

namespace Bud.Server.Application.Organizations;

public interface IOrganizationQueryUseCase
{
    Task<Result<Organization>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<PagedResult<Organization>>> GetAllAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Result<PagedResult<Workspace>>> GetWorkspacesAsync(
        Guid id,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Result<PagedResult<Collaborator>>> GetCollaboratorsAsync(
        Guid id,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
