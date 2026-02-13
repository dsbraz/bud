using Bud.Server.Services;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Application.Teams;

public sealed class TeamQueryUseCase(ITeamService teamService) : ITeamQueryUseCase
{
    public Task<ServiceResult<Team>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => teamService.GetByIdAsync(id, cancellationToken);

    public Task<ServiceResult<PagedResult<Team>>> GetAllAsync(
        Guid? workspaceId,
        Guid? parentTeamId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
        => teamService.GetAllAsync(workspaceId, parentTeamId, search, page, pageSize, cancellationToken);

    public Task<ServiceResult<PagedResult<Team>>> GetSubTeamsAsync(
        Guid id,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
        => teamService.GetSubTeamsAsync(id, page, pageSize, cancellationToken);

    public Task<ServiceResult<PagedResult<Collaborator>>> GetCollaboratorsAsync(
        Guid id,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
        => teamService.GetCollaboratorsAsync(id, page, pageSize, cancellationToken);

    public Task<ServiceResult<List<CollaboratorSummaryDto>>> GetCollaboratorSummariesAsync(
        Guid teamId,
        CancellationToken cancellationToken = default)
        => teamService.GetCollaboratorSummariesAsync(teamId, cancellationToken);

    public Task<ServiceResult<List<CollaboratorSummaryDto>>> GetAvailableCollaboratorsAsync(
        Guid teamId,
        string? search,
        CancellationToken cancellationToken = default)
        => teamService.GetAvailableCollaboratorsAsync(teamId, search, cancellationToken);
}
