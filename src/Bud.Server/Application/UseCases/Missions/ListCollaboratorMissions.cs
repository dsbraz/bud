using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.UseCases.Missions;

public sealed class ListCollaboratorMissions(IMissionRepository missionRepository)
{
    public async Task<Result<Bud.Shared.Contracts.Common.PagedResult<Mission>>> ExecuteAsync(
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
            return Result<Bud.Shared.Contracts.Common.PagedResult<Mission>>.NotFound("Colaborador não encontrado.");
        }

        var teamIds = await missionRepository.GetCollaboratorTeamIdsAsync(collaboratorId, collaborator.TeamId, cancellationToken);
        var workspaceIds = await missionRepository.GetWorkspaceIdsForTeamsAsync(teamIds, cancellationToken);

        var result = await missionRepository.GetMyMissionsAsync(
            collaboratorId,
            collaborator.OrganizationId,
            teamIds,
            workspaceIds,
            search,
            page,
            pageSize,
            cancellationToken);

        return Result<Bud.Shared.Contracts.Common.PagedResult<Mission>>.Success(result.MapPaged(x => x));
    }
}
