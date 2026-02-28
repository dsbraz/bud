using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;

namespace Bud.Server.Application.UseCases.Goals;

public sealed class ListCollaboratorGoals(IGoalRepository goalRepository)
{
    public async Task<Result<PagedResult<Goal>>> ExecuteAsync(
        Guid collaboratorId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);

        var collaborator = await goalRepository.FindCollaboratorForMyGoalsAsync(collaboratorId, cancellationToken);
        if (collaborator is null)
        {
            return Result<PagedResult<Goal>>.NotFound("Colaborador não encontrado.");
        }

        var teamIds = await goalRepository.GetCollaboratorTeamIdsAsync(collaboratorId, collaborator.TeamId, cancellationToken);
        var workspaceIds = await goalRepository.GetWorkspaceIdsForTeamsAsync(teamIds, cancellationToken);

        var result = await goalRepository.GetMyGoalsAsync(
            collaboratorId,
            collaborator.OrganizationId,
            teamIds,
            workspaceIds,
            search,
            page,
            pageSize,
            cancellationToken);

        return Result<PagedResult<Goal>>.Success(result.MapPaged(x => x));
    }
}
