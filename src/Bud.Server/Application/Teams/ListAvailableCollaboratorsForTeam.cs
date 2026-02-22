using Bud.Server.Application.Common;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.Teams;

public sealed class ListAvailableCollaboratorsForTeam(ITeamRepository teamRepository)
{
    public async Task<Result<List<CollaboratorSummaryDto>>> ExecuteAsync(
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
