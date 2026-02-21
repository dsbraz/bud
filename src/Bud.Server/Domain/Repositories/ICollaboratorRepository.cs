using Bud.Server.Application.Projections;
using Bud.Shared.Contracts;
using Bud.Server.Domain.Model;

namespace Bud.Server.Domain.Repositories;

public interface ICollaboratorRepository
{
    Task<Collaborator?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Collaborator?> GetByIdWithCollaboratorTeamsAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<Collaborator>> GetAllAsync(Guid? teamId, string? search, int page, int pageSize, CancellationToken ct = default);
    Task<List<LeaderCollaborator>> GetLeadersAsync(Guid? organizationId, CancellationToken ct = default);
    Task<List<CollaboratorHierarchyNode>> GetSubordinatesAsync(Guid collaboratorId, int maxDepth, CancellationToken ct = default);
    Task<List<TeamSummary>> GetTeamsAsync(Guid collaboratorId, CancellationToken ct = default);
    Task<List<TeamSummary>> GetAvailableTeamsAsync(Guid collaboratorId, Guid organizationId, string? search, int limit, CancellationToken ct = default);
    Task<List<CollaboratorSummary>> GetSummariesAsync(string? search, int limit, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
    Task<bool> IsEmailUniqueAsync(string email, Guid? excludeId, CancellationToken ct = default);
    Task<bool> HasSubordinatesAsync(Guid collaboratorId, CancellationToken ct = default);
    Task<bool> IsOrganizationOwnerAsync(Guid collaboratorId, CancellationToken ct = default);
    Task<bool> HasMissionsAsync(Guid collaboratorId, CancellationToken ct = default);
    Task<int> CountTeamsByIdsAndOrganizationAsync(List<Guid> teamIds, Guid organizationId, CancellationToken ct = default);
    Task<int> CountByIdsAndOrganizationAsync(List<Guid> ids, Guid organizationId, CancellationToken ct = default);
    Task<bool> IsValidLeaderAsync(Guid leaderId, Guid? requiredOrganizationId, CancellationToken ct = default);
    Task AddAsync(Collaborator entity, CancellationToken ct = default);
    Task RemoveAsync(Collaborator entity, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
