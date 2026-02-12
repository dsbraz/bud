using Bud.Shared.Contracts;
using Bud.Shared.Models;

namespace Bud.Server.Application.Abstractions;

public interface ICollaboratorQueryService
{
    Task<ServiceResult<Collaborator>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ServiceResult<PagedResult<Collaborator>>> GetAllAsync(Guid? teamId, string? search, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ServiceResult<List<LeaderCollaboratorResponse>>> GetLeadersAsync(Guid? organizationId = null, CancellationToken cancellationToken = default);
    Task<ServiceResult<List<CollaboratorHierarchyNodeDto>>> GetSubordinatesAsync(Guid collaboratorId, int maxDepth = 5, CancellationToken cancellationToken = default);
    Task<ServiceResult<List<TeamSummaryDto>>> GetTeamsAsync(Guid collaboratorId, CancellationToken cancellationToken = default);
    Task<ServiceResult<List<TeamSummaryDto>>> GetAvailableTeamsAsync(Guid collaboratorId, string? search = null, CancellationToken cancellationToken = default);
}
