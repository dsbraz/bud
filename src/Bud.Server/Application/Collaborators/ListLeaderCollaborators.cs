using Bud.Server.Application.Common;
using Bud.Server.Domain.Repositories;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.Collaborators;

public sealed class ListLeaderCollaborators(ICollaboratorRepository collaboratorRepository)
{
    public async Task<Result<List<LeaderCollaboratorResponse>>> ExecuteAsync(
        Guid? organizationId,
        CancellationToken cancellationToken = default)
    {
        var leaders = await collaboratorRepository.GetLeadersAsync(organizationId, cancellationToken);
        return Result<List<LeaderCollaboratorResponse>>.Success(leaders.Select(c => c.ToContractAsLeader()).ToList());
    }
}
