using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Domain.Repositories;

namespace Bud.Server.Application.UseCases.Collaborators;

public sealed class ListCollaboratorTeams(ICollaboratorRepository collaboratorRepository)
{
    public async Task<Result<List<CollaboratorTeamResponse>>> ExecuteAsync(
        Guid collaboratorId,
        CancellationToken cancellationToken = default)
    {
        if (!await collaboratorRepository.ExistsAsync(collaboratorId, cancellationToken))
        {
            return Result<List<CollaboratorTeamResponse>>.NotFound("Colaborador não encontrado.");
        }

        var teams = await collaboratorRepository.GetTeamsAsync(collaboratorId, cancellationToken);
        return Result<List<CollaboratorTeamResponse>>.Success(teams.Select(t => t.ToResponse()).ToList());
    }
}
