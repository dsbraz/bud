using Bud.Application.Common;
using Bud.Shared.Contracts;

namespace Bud.Application.Features.Collaborators.UseCases;

public sealed class ListLeaderCollaborators(ICollaboratorRepository collaboratorRepository)
{
    public async Task<Result<List<CollaboratorLeaderResponse>>> ExecuteAsync(
        Guid? organizationId,
        CancellationToken cancellationToken = default)
    {
        var leaders = await collaboratorRepository.GetLeadersAsync(organizationId, cancellationToken);
        return Result<List<CollaboratorLeaderResponse>>.Success(leaders.Select(c => c.ToLeaderResponse()).ToList());
    }
}
