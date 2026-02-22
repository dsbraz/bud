using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Domain.Repositories;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.UseCases.Collaborators;

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
