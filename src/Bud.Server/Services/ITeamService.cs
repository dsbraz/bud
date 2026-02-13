using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Services;

public interface ITeamService
{
    Task<ServiceResult<Team>> CreateAsync(CreateTeamRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<Team>> UpdateAsync(Guid id, UpdateTeamRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ServiceResult> UpdateCollaboratorsAsync(Guid teamId, UpdateTeamCollaboratorsRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<Team>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ServiceResult<PagedResult<Team>>> GetAllAsync(Guid? workspaceId, Guid? parentTeamId, string? search, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ServiceResult<PagedResult<Team>>> GetSubTeamsAsync(Guid id, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ServiceResult<PagedResult<Collaborator>>> GetCollaboratorsAsync(Guid id, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ServiceResult<List<CollaboratorSummaryDto>>> GetCollaboratorSummariesAsync(Guid teamId, CancellationToken cancellationToken = default);
    Task<ServiceResult<List<CollaboratorSummaryDto>>> GetAvailableCollaboratorsAsync(Guid teamId, string? search = null, CancellationToken cancellationToken = default);
}
