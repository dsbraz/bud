using Bud.Server.Infrastructure.Repositories;
using Bud.Server.Infrastructure.Services;
using Bud.Server.Application.Common;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Application.Missions;

public sealed class MissionQueryUseCase(
    IMissionRepository missionRepository,
    IMissionProgressService missionProgressService) : IMissionQueryUseCase
{
    public async Task<Result<Mission>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var mission = await missionRepository.GetByIdReadOnlyAsync(id, cancellationToken);
        return mission is null
            ? Result<Mission>.NotFound("Missão não encontrada.")
            : Result<Mission>.Success(mission);
    }

    public async Task<Result<PagedResult<Mission>>> GetAllAsync(
        MissionScopeType? scopeType,
        Guid? scopeId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);
        var result = await missionRepository.GetAllAsync(scopeType, scopeId, search, page, pageSize, cancellationToken);
        return Result<PagedResult<Mission>>.Success(result);
    }

    public async Task<Result<PagedResult<Mission>>> GetMyMissionsAsync(
        Guid collaboratorId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);

        var collaborator = await missionRepository.FindCollaboratorForMyMissionsAsync(collaboratorId, cancellationToken);
        if (collaborator is null)
        {
            return Result<PagedResult<Mission>>.NotFound("Colaborador não encontrado.");
        }

        var teamIds = await missionRepository.GetCollaboratorTeamIdsAsync(collaboratorId, collaborator.TeamId, cancellationToken);
        var workspaceIds = await missionRepository.GetWorkspaceIdsForTeamsAsync(teamIds, cancellationToken);

        var result = await missionRepository.GetMyMissionsAsync(
            collaboratorId, collaborator.OrganizationId, teamIds, workspaceIds, search, page, pageSize, cancellationToken);

        return Result<PagedResult<Mission>>.Success(result);
    }

    public async Task<Result<List<MissionProgressDto>>> GetProgressAsync(
        List<Guid> missionIds,
        CancellationToken cancellationToken = default)
    {
        var result = await missionProgressService.GetProgressAsync(missionIds, cancellationToken);
        if (!result.IsSuccess)
        {
            return Result<List<MissionProgressDto>>.Failure(result.Error ?? "Falha ao calcular progresso das missões.", result.ErrorType);
        }

        return Result<List<MissionProgressDto>>.Success(
            result.Value!.Select(p => p.ToContract()).ToList());
    }

    public async Task<Result<PagedResult<MissionMetric>>> GetMetricsAsync(
        Guid id,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);

        var missionExists = await missionRepository.ExistsAsync(id, cancellationToken);
        if (!missionExists)
        {
            return Result<PagedResult<MissionMetric>>.NotFound("Missão não encontrada.");
        }

        var result = await missionRepository.GetMetricsAsync(id, page, pageSize, cancellationToken);
        return Result<PagedResult<MissionMetric>>.Success(result);
    }
}
