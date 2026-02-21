using Bud.Server.Infrastructure.Repositories;
using Bud.Server.Application.Common;
using Bud.Server.Domain.ReadModels;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Application.Teams;

public sealed class TeamQuery(ITeamRepository teamRepository)
{
    public async Task<Result<Team>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var team = await teamRepository.GetByIdAsync(id, cancellationToken);
        return team is null
            ? Result<Team>.NotFound("Time não encontrado.")
            : Result<Team>.Success(team);
    }

    public async Task<Result<PagedResult<Team>>> GetAllAsync(
        Guid? workspaceId,
        Guid? parentTeamId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);
        var result = await teamRepository.GetAllAsync(workspaceId, parentTeamId, search, page, pageSize, cancellationToken);
        return Result<PagedResult<Team>>.Success(result);
    }

    public async Task<Result<PagedResult<Team>>> GetSubTeamsAsync(
        Guid id,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);

        if (!await teamRepository.ExistsAsync(id, cancellationToken))
        {
            return Result<PagedResult<Team>>.NotFound("Time não encontrado.");
        }

        var result = await teamRepository.GetSubTeamsAsync(id, page, pageSize, cancellationToken);
        return Result<PagedResult<Team>>.Success(result);
    }

    public async Task<Result<PagedResult<Collaborator>>> GetCollaboratorsAsync(
        Guid id,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);

        if (!await teamRepository.ExistsAsync(id, cancellationToken))
        {
            return Result<PagedResult<Collaborator>>.NotFound("Time não encontrado.");
        }

        var result = await teamRepository.GetCollaboratorsAsync(id, page, pageSize, cancellationToken);
        return Result<PagedResult<Collaborator>>.Success(result);
    }

    public async Task<Result<List<CollaboratorSummaryDto>>> GetCollaboratorSummariesAsync(
        Guid teamId,
        CancellationToken cancellationToken = default)
    {
        if (!await teamRepository.ExistsAsync(teamId, cancellationToken))
        {
            return Result<List<CollaboratorSummaryDto>>.NotFound("Time não encontrado.");
        }

        var summaries = await teamRepository.GetCollaboratorSummariesAsync(teamId, cancellationToken);
        return Result<List<CollaboratorSummaryDto>>.Success(summaries.Select(c => c.ToContract()).ToList());
    }

    public async Task<Result<List<CollaboratorSummaryDto>>> GetAvailableCollaboratorsAsync(
        Guid teamId,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var team = await teamRepository.GetByIdAsync(teamId, cancellationToken);
        if (team is null)
        {
            return Result<List<CollaboratorSummaryDto>>.NotFound("Time não encontrado.");
        }

        var summaries = await teamRepository.GetAvailableCollaboratorsAsync(teamId, team.OrganizationId, search, 50, cancellationToken);
        return Result<List<CollaboratorSummaryDto>>.Success(summaries.Select(c => c.ToContract()).ToList());
    }
}
