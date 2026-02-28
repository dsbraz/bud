using Bud.Server.Domain.Model;

namespace Bud.Server.Domain.Repositories;

public interface IGoalRepository
{
    Task<Goal?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Goal?> GetByIdReadOnlyAsync(Guid id, CancellationToken ct = default);
    Task<Bud.Shared.Contracts.Common.PagedResult<Goal>> GetAllAsync(
        Guid? parentId, GoalScopeType? scopeType, Guid? scopeId, string? search,
        int page, int pageSize, CancellationToken ct = default);
    Task<Bud.Shared.Contracts.Common.PagedResult<Goal>> GetMyGoalsAsync(
        Guid collaboratorId, Guid organizationId,
        List<Guid> teamIds, List<Guid> workspaceIds, string? search,
        int page, int pageSize, CancellationToken ct = default);
    Task<Collaborator?> FindCollaboratorForMyGoalsAsync(Guid collaboratorId, CancellationToken ct = default);
    Task<List<Guid>> GetCollaboratorTeamIdsAsync(Guid collaboratorId, Guid? primaryTeamId, CancellationToken ct = default);
    Task<List<Guid>> GetWorkspaceIdsForTeamsAsync(List<Guid> teamIds, CancellationToken ct = default);
    Task<Bud.Shared.Contracts.Common.PagedResult<Goal>> GetChildrenAsync(Guid parentId, int page, int pageSize, CancellationToken ct = default);
    Task<Bud.Shared.Contracts.Common.PagedResult<Indicator>> GetIndicatorsAsync(Guid goalId, int page, int pageSize, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Goal entity, CancellationToken ct = default);
    Task RemoveAsync(Goal entity, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
