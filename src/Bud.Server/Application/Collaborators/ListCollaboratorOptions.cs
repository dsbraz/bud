using Bud.Server.Application.Common;
using Bud.Server.Domain.Repositories;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.Collaborators;

public sealed class ListCollaboratorOptions(ICollaboratorRepository collaboratorRepository)
{
    public async Task<Result<List<CollaboratorSummaryDto>>> ExecuteAsync(
        string? search,
        CancellationToken cancellationToken = default)
    {
        var summaries = await collaboratorRepository.GetSummariesAsync(search, 50, cancellationToken);
        return Result<List<CollaboratorSummaryDto>>.Success(summaries.Select(c => c.ToContract()).ToList());
    }
}
