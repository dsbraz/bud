using Bud.Server.Application.Common;
using Bud.Server.Domain.Repositories;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.Collaborators;

public sealed class ListCollaboratorTeams(ICollaboratorRepository collaboratorRepository)
{
    public async Task<Result<List<TeamSummaryDto>>> ExecuteAsync(
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
}
