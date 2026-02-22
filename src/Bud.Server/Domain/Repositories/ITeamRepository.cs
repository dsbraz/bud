using Bud.Server.Domain.Model;

namespace Bud.Server.Domain.Repositories;

public interface ITeamRepository
{
    Task<Team?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Team?> GetByIdWithCollaboratorTeamsAsync(Guid id, CancellationToken ct = default);
    Task<Bud.Shared.Contracts.Common.PagedResult<Team>> GetAllAsync(Guid? workspaceId, Guid? parentTeamId, string? search, int page, int pageSize, CancellationToken ct = default);
    Task<Bud.Shared.Contracts.Common.PagedResult<Team>> GetSubTeamsAsync(Guid teamId, int page, int pageSize, CancellationToken ct = default);
    Task<Bud.Shared.Contracts.Common.PagedResult<Collaborator>> GetCollaboratorsAsync(Guid teamId, int page, int pageSize, CancellationToken ct = default);
    Task<List<Collaborator>> GetCollaboratorSummariesAsync(Guid teamId, CancellationToken ct = default);
    Task<List<Collaborator>> GetAvailableCollaboratorsAsync(Guid teamId, Guid organizationId, string? search, int limit, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
    Task<bool> HasSubTeamsAsync(Guid teamId, CancellationToken ct = default);
    Task<bool> HasMissionsAsync(Guid teamId, CancellationToken ct = default);
    Task AddAsync(Team entity, CancellationToken ct = default);
    Task RemoveAsync(Team entity, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
