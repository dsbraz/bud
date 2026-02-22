using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Domain.Repositories;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.UseCases.Collaborators;

public sealed class ListCollaboratorOptions(ICollaboratorRepository collaboratorRepository)
{
    public async Task<Result<List<CollaboratorLookupResponse>>> ExecuteAsync(
        string? search,
        CancellationToken cancellationToken = default)
    {
        var summaries = await collaboratorRepository.GetSummariesAsync(search, 50, cancellationToken);
        return Result<List<CollaboratorLookupResponse>>.Success(summaries.Select(c => c.ToResponse()).ToList());
    }
}
