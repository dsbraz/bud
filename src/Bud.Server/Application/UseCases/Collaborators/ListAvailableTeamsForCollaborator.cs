using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Domain.Repositories;

namespace Bud.Server.Application.UseCases.Collaborators;

public sealed class ListAvailableTeamsForCollaborator(ICollaboratorRepository collaboratorRepository)
{
    public async Task<Result<List<CollaboratorTeamEligibleResponse>>> ExecuteAsync(
        Guid collaboratorId,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var collaborator = await collaboratorRepository.GetByIdAsync(collaboratorId, cancellationToken);
        if (collaborator is null)
        {
            return Result<List<CollaboratorTeamEligibleResponse>>.NotFound(UserErrorMessages.CollaboratorNotFound);
        }

        var teams = await collaboratorRepository.GetEligibleTeamsForAssignmentAsync(
            collaboratorId,
            collaborator.OrganizationId,
            search,
            50,
            cancellationToken);
        return Result<List<CollaboratorTeamEligibleResponse>>.Success(teams.Select(t => t.ToCollaboratorTeamEligibleResponse()).ToList());
    }
}
