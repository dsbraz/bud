using Bud.Application.Common;
using Bud.Application.Mapping;
using Bud.Domain.Repositories;
using Bud.Shared.Contracts;

namespace Bud.Application.UseCases.Teams;

public sealed class GetTeamCollaboratorLookup(ITeamRepository teamRepository)
{
    public async Task<Result<List<CollaboratorLookupResponse>>> ExecuteAsync(
        Guid teamId,
        CancellationToken cancellationToken = default)
    {
        if (!await teamRepository.ExistsAsync(teamId, cancellationToken))
        {
            return Result<List<CollaboratorLookupResponse>>.NotFound(UserErrorMessages.TeamNotFound);
        }

        var summaries = await teamRepository.GetCollaboratorLookupAsync(teamId, cancellationToken);
        return Result<List<CollaboratorLookupResponse>>.Success(summaries.Select(c => c.ToResponse()).ToList());
    }
}
