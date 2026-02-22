using Bud.Server.Application.Common;
using Bud.Server.Domain.Repositories;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.Teams;

public sealed class ListTeamCollaboratorOptions(ITeamRepository teamRepository)
{
    public async Task<Result<List<CollaboratorSummaryDto>>> ExecuteAsync(
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
}
