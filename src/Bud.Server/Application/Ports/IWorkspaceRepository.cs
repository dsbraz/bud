using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Application.Ports;

public interface IWorkspaceRepository
{
    Task<Workspace?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<Workspace>> GetAllAsync(Guid? organizationId, string? search, int page, int pageSize, CancellationToken ct = default);
    Task<PagedResult<Team>> GetTeamsAsync(Guid workspaceId, int page, int pageSize, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
    Task<bool> IsNameUniqueAsync(Guid organizationId, string name, Guid? excludeId = null, CancellationToken ct = default);
    Task<bool> HasMissionsAsync(Guid workspaceId, CancellationToken ct = default);
    Task AddAsync(Workspace entity, CancellationToken ct = default);
    Task RemoveAsync(Workspace entity, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
