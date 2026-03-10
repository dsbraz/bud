using Bud.Application.Common;
using Bud.Shared.Contracts;

namespace Bud.Application.Collaborators;

public sealed class GetCollaboratorLookup(ICollaboratorRepository collaboratorRepository)
{
    public async Task<Result<List<CollaboratorLookupResponse>>> ExecuteAsync(
        string? search,
        CancellationToken cancellationToken = default)
    {
        var summaries = await collaboratorRepository.GetLookupAsync(search, 50, cancellationToken);
        return Result<List<CollaboratorLookupResponse>>.Success(summaries.Select(c => c.ToResponse()).ToList());
    }
}
