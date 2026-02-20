using Bud.Server.Domain.ReadModels;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Services;

public interface ICollaboratorService
{
    Task<ServiceResult<Collaborator>> CreateAsync(CreateCollaboratorRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<Collaborator>> UpdateAsync(Guid id, UpdateCollaboratorRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ServiceResult> UpdateTeamsAsync(Guid collaboratorId, UpdateCollaboratorTeamsRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<Collaborator>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ServiceResult<PagedResult<Collaborator>>> GetAllAsync(Guid? teamId, string? search, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ServiceResult<List<LeaderCollaborator>>> GetLeadersAsync(Guid? organizationId = null, CancellationToken cancellationToken = default);
    Task<ServiceResult<List<CollaboratorHierarchyNode>>> GetSubordinatesAsync(Guid collaboratorId, int maxDepth = 5, CancellationToken cancellationToken = default);
    Task<ServiceResult<List<TeamSummary>>> GetTeamsAsync(Guid collaboratorId, CancellationToken cancellationToken = default);
    Task<ServiceResult<List<TeamSummary>>> GetAvailableTeamsAsync(Guid collaboratorId, string? search = null, CancellationToken cancellationToken = default);
    Task<ServiceResult<List<CollaboratorSummary>>> GetSummariesAsync(string? search = null, CancellationToken cancellationToken = default);
}
