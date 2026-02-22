using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Domain.Repositories;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.UseCases.Teams;

public sealed class ListTeamCollaboratorOptions(ITeamRepository teamRepository)
{
    public async Task<Result<List<CollaboratorLookupResponse>>> ExecuteAsync(
        Guid teamId,
        CancellationToken cancellationToken = default)
    {
        if (!await teamRepository.ExistsAsync(teamId, cancellationToken))
        {
            return Result<List<CollaboratorLookupResponse>>.NotFound("Time não encontrado.");
        }

        var summaries = await teamRepository.GetCollaboratorSummariesAsync(teamId, cancellationToken);
        return Result<List<CollaboratorLookupResponse>>.Success(summaries.Select(c => c.ToResponse()).ToList());
    }
}
