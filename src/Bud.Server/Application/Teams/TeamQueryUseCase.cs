using Bud.Server.Services;
using Bud.Server.Application.Common;
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

    public async Task<ServiceResult<List<CollaboratorSummaryDto>>> GetCollaboratorSummariesAsync(
        Guid teamId,
        CancellationToken cancellationToken = default)
    {
        var result = await teamService.GetCollaboratorSummariesAsync(teamId, cancellationToken);
        if (!result.IsSuccess)
        {
            return ServiceResult<List<CollaboratorSummaryDto>>.Failure(result.Error ?? "Falha ao listar colaboradores do time.", result.ErrorType);
        }

        return ServiceResult<List<CollaboratorSummaryDto>>.Success(result.Value!.Select(c => c.ToContract()).ToList());
    }

    public async Task<ServiceResult<List<CollaboratorSummaryDto>>> GetAvailableCollaboratorsAsync(
        Guid teamId,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var result = await teamService.GetAvailableCollaboratorsAsync(teamId, search, cancellationToken);
        if (!result.IsSuccess)
        {
            return ServiceResult<List<CollaboratorSummaryDto>>.Failure(result.Error ?? "Falha ao listar colaboradores disponíveis.", result.ErrorType);
        }

        return ServiceResult<List<CollaboratorSummaryDto>>.Success(result.Value!.Select(c => c.ToContract()).ToList());
    }
}
