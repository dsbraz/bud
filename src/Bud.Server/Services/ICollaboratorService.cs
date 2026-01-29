using Bud.Shared.Contracts;
using Bud.Shared.Models;

namespace Bud.Server.Services;

public interface ICollaboratorService
{
    Task<ServiceResult<Collaborator>> CreateAsync(CreateCollaboratorRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<Collaborator>> UpdateAsync(Guid id, UpdateCollaboratorRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ServiceResult<Collaborator>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ServiceResult<PagedResult<Collaborator>>> GetAllAsync(Guid? teamId, string? search, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ServiceResult<List<LeaderCollaboratorResponse>>> GetLeadersAsync(CancellationToken cancellationToken = default);
}
