using Bud.Application.Common;
using Bud.Shared.Contracts;

namespace Bud.Application.Features.Teams.UseCases;

public sealed class ListAvailableCollaboratorsForTeam(ITeamRepository teamRepository)
{
    public async Task<Result<List<TeamCollaboratorEligibleResponse>>> ExecuteAsync(
        Guid teamId,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var team = await teamRepository.GetByIdAsync(teamId, cancellationToken);
        if (team is null)
        {
            return Result<List<TeamCollaboratorEligibleResponse>>.NotFound(UserErrorMessages.TeamNotFound);
        }

        var summaries = await teamRepository.GetEligibleCollaboratorsForAssignmentAsync(teamId, team.OrganizationId, search, 50, cancellationToken);
        return Result<List<TeamCollaboratorEligibleResponse>>.Success(summaries.Select(c => c.ToTeamCollaboratorEligibleResponse()).ToList());
    }
}
