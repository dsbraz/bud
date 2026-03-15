
namespace Bud.Application.Features.Collaborators;

public interface ICollaboratorRepository
{
    Task<Collaborator?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Collaborator?> GetByIdWithCollaboratorTeamsAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<Collaborator>> GetAllAsync(Guid? teamId, string? search, int page, int pageSize, CancellationToken ct = default);
    Task<List<Collaborator>> GetLeadersAsync(Guid? organizationId, CancellationToken ct = default);
    Task<List<Collaborator>> GetSubordinatesAsync(Guid collaboratorId, int maxDepth, CancellationToken ct = default);
    Task<List<Team>> GetTeamsAsync(Guid collaboratorId, CancellationToken ct = default);
    Task<List<Team>> GetEligibleTeamsForAssignmentAsync(Guid collaboratorId, Guid organizationId, string? search, int limit, CancellationToken ct = default);
    Task<List<Collaborator>> GetLookupAsync(string? search, int limit, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
    Task<bool> IsEmailUniqueAsync(string email, Guid? excludeId, CancellationToken ct = default);
    Task<bool> HasSubordinatesAsync(Guid collaboratorId, CancellationToken ct = default);
    Task<bool> IsOrganizationOwnerAsync(Guid collaboratorId, CancellationToken ct = default);
    Task<bool> HasGoalsAsync(Guid collaboratorId, CancellationToken ct = default);
    Task<int> CountTeamsByIdsAndOrganizationAsync(List<Guid> teamIds, Guid organizationId, CancellationToken ct = default);
    Task<int> CountByIdsAndOrganizationAsync(List<Guid> ids, Guid organizationId, CancellationToken ct = default);
    Task<bool> IsValidLeaderAsync(Guid leaderId, Guid? requiredOrganizationId, CancellationToken ct = default);
    Task AddAsync(Collaborator entity, CancellationToken ct = default);
    Task RemoveAsync(Collaborator entity, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
