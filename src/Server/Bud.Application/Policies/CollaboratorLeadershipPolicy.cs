using Bud.Application.Common;
using Bud.Domain.Model;
using Bud.Domain.Repositories;

namespace Bud.Application.Policies;

internal static class CollaboratorLeadershipPolicy
{
    public static async Task<Result<T>?> ValidateLeaderForOrganizationAsync<T>(
        ICollaboratorRepository collaboratorRepository,
        Guid leaderId,
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        var leader = await collaboratorRepository.GetByIdAsync(leaderId, cancellationToken);
        if (leader is null)
        {
            return Result<T>.NotFound(UserErrorMessages.LeaderNotFound);
        }

        try
        {
            leader.EnsureCanLeadOrganization(organizationId);
            return null;
        }
        catch (DomainInvariantException ex)
        {
            return Result<T>.Failure(ex.Message, ErrorType.Validation);
        }
    }
}
