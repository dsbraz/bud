using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using Bud.Server.Application.Common;

namespace Bud.Server.Application.Teams;

public interface ITeamQueryUseCase
{
    Task<Result<Team>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<PagedResult<Team>>> GetAllAsync(
        Guid? workspaceId,
        Guid? parentTeamId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Result<PagedResult<Team>>> GetSubTeamsAsync(
        Guid id,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Result<PagedResult<Collaborator>>> GetCollaboratorsAsync(
        Guid id,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Result<List<CollaboratorSummaryDto>>> GetCollaboratorSummariesAsync(
        Guid teamId,
        CancellationToken cancellationToken = default);

    Task<Result<List<CollaboratorSummaryDto>>> GetAvailableCollaboratorsAsync(
        Guid teamId,
        string? search,
        CancellationToken cancellationToken = default);
}
