using Bud.Application.Common;
using Bud.Application.Mapping;
using Bud.Domain.Repositories;
using Bud.Shared.Contracts;

namespace Bud.Application.UseCases.Collaborators;

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
