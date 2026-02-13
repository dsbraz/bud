using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Application.Teams;

public interface ITeamQueryUseCase
{
    Task<ServiceResult<Team>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ServiceResult<PagedResult<Team>>> GetAllAsync(
        Guid? workspaceId,
        Guid? parentTeamId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<PagedResult<Team>>> GetSubTeamsAsync(
        Guid id,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<PagedResult<Collaborator>>> GetCollaboratorsAsync(
        Guid id,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<List<CollaboratorSummaryDto>>> GetCollaboratorSummariesAsync(
        Guid teamId,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<List<CollaboratorSummaryDto>>> GetAvailableCollaboratorsAsync(
        Guid teamId,
        string? search,
        CancellationToken cancellationToken = default);
}
