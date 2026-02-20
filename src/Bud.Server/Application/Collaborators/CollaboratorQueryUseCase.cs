using Bud.Server.Services;
using Bud.Server.Application.Common;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Application.Collaborators;

public sealed class CollaboratorQueryUseCase(ICollaboratorService collaboratorService) : ICollaboratorQueryUseCase
{
    public Task<ServiceResult<Collaborator>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => collaboratorService.GetByIdAsync(id, cancellationToken);

    public async Task<ServiceResult<List<LeaderCollaboratorResponse>>> GetLeadersAsync(
        Guid? organizationId,
        CancellationToken cancellationToken = default)
    {
        var result = await collaboratorService.GetLeadersAsync(organizationId, cancellationToken);
        if (!result.IsSuccess)
        {
            return ServiceResult<List<LeaderCollaboratorResponse>>.Failure(result.Error ?? "Falha ao listar líderes.", result.ErrorType);
        }

        return ServiceResult<List<LeaderCollaboratorResponse>>.Success(result.Value!.Select(c => c.ToContract()).ToList());
    }

    public Task<ServiceResult<PagedResult<Collaborator>>> GetAllAsync(
        Guid? teamId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
        => collaboratorService.GetAllAsync(teamId, search, page, pageSize, cancellationToken);

    public async Task<ServiceResult<List<CollaboratorHierarchyNodeDto>>> GetSubordinatesAsync(
        Guid collaboratorId,
        CancellationToken cancellationToken = default)
    {
        var result = await collaboratorService.GetSubordinatesAsync(collaboratorId, cancellationToken: cancellationToken);
        if (!result.IsSuccess)
        {
            return ServiceResult<List<CollaboratorHierarchyNodeDto>>.Failure(result.Error ?? "Falha ao listar subordinados.", result.ErrorType);
        }

        return ServiceResult<List<CollaboratorHierarchyNodeDto>>.Success(result.Value!.Select(c => c.ToContract()).ToList());
    }

    public async Task<ServiceResult<List<TeamSummaryDto>>> GetTeamsAsync(
        Guid collaboratorId,
        CancellationToken cancellationToken = default)
    {
        var result = await collaboratorService.GetTeamsAsync(collaboratorId, cancellationToken);
        if (!result.IsSuccess)
        {
            return ServiceResult<List<TeamSummaryDto>>.Failure(result.Error ?? "Falha ao listar equipes do colaborador.", result.ErrorType);
        }

        return ServiceResult<List<TeamSummaryDto>>.Success(result.Value!.Select(t => t.ToContract()).ToList());
    }

    public async Task<ServiceResult<List<TeamSummaryDto>>> GetAvailableTeamsAsync(
        Guid collaboratorId,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var result = await collaboratorService.GetAvailableTeamsAsync(collaboratorId, search, cancellationToken);
        if (!result.IsSuccess)
        {
            return ServiceResult<List<TeamSummaryDto>>.Failure(result.Error ?? "Falha ao listar equipes disponíveis.", result.ErrorType);
        }

        return ServiceResult<List<TeamSummaryDto>>.Success(result.Value!.Select(t => t.ToContract()).ToList());
    }

    public async Task<ServiceResult<List<CollaboratorSummaryDto>>> GetSummariesAsync(
        string? search,
        CancellationToken cancellationToken = default)
    {
        var result = await collaboratorService.GetSummariesAsync(search, cancellationToken);
        if (!result.IsSuccess)
        {
            return ServiceResult<List<CollaboratorSummaryDto>>.Failure(result.Error ?? "Falha ao listar colaboradores.", result.ErrorType);
        }

        return ServiceResult<List<CollaboratorSummaryDto>>.Success(result.Value!.Select(c => c.ToContract()).ToList());
    }
}
