using Bud.Server.Infrastructure.Repositories;
using Bud.Server.Application.Common;
using Bud.Server.Domain.ReadModels;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Application.Collaborators;

public sealed class CollaboratorQueryUseCase(ICollaboratorRepository collaboratorRepository) : ICollaboratorQueryUseCase
{
    public async Task<Result<Collaborator>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var collaborator = await collaboratorRepository.GetByIdAsync(id, cancellationToken);
        return collaborator is null
            ? Result<Collaborator>.NotFound("Colaborador não encontrado.")
            : Result<Collaborator>.Success(collaborator);
    }

    public async Task<Result<List<LeaderCollaboratorResponse>>> GetLeadersAsync(
        Guid? organizationId,
        CancellationToken cancellationToken = default)
    {
        var leaders = await collaboratorRepository.GetLeadersAsync(organizationId, cancellationToken);
        return Result<List<LeaderCollaboratorResponse>>.Success(leaders.Select(c => c.ToContract()).ToList());
    }

    public async Task<Result<PagedResult<Collaborator>>> GetAllAsync(
        Guid? teamId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);
        var result = await collaboratorRepository.GetAllAsync(teamId, search, page, pageSize, cancellationToken);
        return Result<PagedResult<Collaborator>>.Success(result);
    }

    public async Task<Result<List<CollaboratorHierarchyNodeDto>>> GetSubordinatesAsync(
        Guid collaboratorId,
        CancellationToken cancellationToken = default)
    {
        if (!await collaboratorRepository.ExistsAsync(collaboratorId, cancellationToken))
        {
            return Result<List<CollaboratorHierarchyNodeDto>>.NotFound("Colaborador não encontrado.");
        }

        var nodes = await collaboratorRepository.GetSubordinatesAsync(collaboratorId, 5, cancellationToken);
        return Result<List<CollaboratorHierarchyNodeDto>>.Success(nodes.Select(c => c.ToContract()).ToList());
    }

    public async Task<Result<List<TeamSummaryDto>>> GetTeamsAsync(
        Guid collaboratorId,
        CancellationToken cancellationToken = default)
    {
        if (!await collaboratorRepository.ExistsAsync(collaboratorId, cancellationToken))
        {
            return Result<List<TeamSummaryDto>>.NotFound("Colaborador não encontrado.");
        }

        var teams = await collaboratorRepository.GetTeamsAsync(collaboratorId, cancellationToken);
        return Result<List<TeamSummaryDto>>.Success(teams.Select(t => t.ToContract()).ToList());
    }

    public async Task<Result<List<TeamSummaryDto>>> GetAvailableTeamsAsync(
        Guid collaboratorId,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var collaborator = await collaboratorRepository.GetByIdAsync(collaboratorId, cancellationToken);
        if (collaborator is null)
        {
            return Result<List<TeamSummaryDto>>.NotFound("Colaborador não encontrado.");
        }

        var teams = await collaboratorRepository.GetAvailableTeamsAsync(
            collaboratorId, collaborator.OrganizationId, search, 50, cancellationToken);
        return Result<List<TeamSummaryDto>>.Success(teams.Select(t => t.ToContract()).ToList());
    }

    public async Task<Result<List<CollaboratorSummaryDto>>> GetSummariesAsync(
        string? search,
        CancellationToken cancellationToken = default)
    {
        var summaries = await collaboratorRepository.GetSummariesAsync(search, 50, cancellationToken);
        return Result<List<CollaboratorSummaryDto>>.Success(summaries.Select(c => c.ToContract()).ToList());
    }
}
