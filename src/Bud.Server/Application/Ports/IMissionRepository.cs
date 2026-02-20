using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Application.Ports;

public interface IMissionRepository
{
    Task<Mission?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Mission?> GetByIdReadOnlyAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<Mission>> GetAllAsync(
        MissionScopeType? scopeType, Guid? scopeId, string? search,
        int page, int pageSize, CancellationToken ct = default);
    Task<PagedResult<Mission>> GetMyMissionsAsync(
        Guid collaboratorId, Guid organizationId,
        List<Guid> teamIds, List<Guid> workspaceIds, string? search,
        int page, int pageSize, CancellationToken ct = default);
    Task<Collaborator?> FindCollaboratorForMyMissionsAsync(Guid collaboratorId, CancellationToken ct = default);
    Task<List<Guid>> GetCollaboratorTeamIdsAsync(Guid collaboratorId, Guid? primaryTeamId, CancellationToken ct = default);
    Task<List<Guid>> GetWorkspaceIdsForTeamsAsync(List<Guid> teamIds, CancellationToken ct = default);
    Task<PagedResult<MissionMetric>> GetMetricsAsync(Guid missionId, int page, int pageSize, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Mission entity, CancellationToken ct = default);
    Task RemoveAsync(Mission entity, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
