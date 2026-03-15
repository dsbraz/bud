using Bud.Application.Common;

namespace Bud.Application.Features.Collaborators;

public sealed class ListCollaboratorTeams(ICollaboratorRepository collaboratorRepository)
{
    public async Task<Result<List<CollaboratorTeamResponse>>> ExecuteAsync(
        Guid collaboratorId,
        CancellationToken cancellationToken = default)
    {
        if (!await collaboratorRepository.ExistsAsync(collaboratorId, cancellationToken))
        {
            return Result<List<CollaboratorTeamResponse>>.NotFound(UserErrorMessages.CollaboratorNotFound);
        }

        var teams = await collaboratorRepository.GetTeamsAsync(collaboratorId, cancellationToken);
        return Result<List<CollaboratorTeamResponse>>.Success(teams.Select(t => t.ToResponse()).ToList());
    }
}
