using Bud.Server.Application.Common;
using Bud.Server.Domain.Repositories;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.Collaborators;

public sealed class ListAvailableTeamsForCollaborator(ICollaboratorRepository collaboratorRepository)
{
    public async Task<Result<List<TeamSummaryDto>>> ExecuteAsync(
        Guid collaboratorId,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var collaborator = await collaboratorRepository.GetByIdAsync(collaboratorId, cancellationToken);
        if (collaborator is null)
        {
            return Result<List<TeamSummaryDto>>.NotFound("Colaborador não encontrado.");
        }

        var teams = await collaboratorRepository.GetAvailableTeamsAsync(
            collaboratorId,
            collaborator.OrganizationId,
            search,
            50,
            cancellationToken);
        return Result<List<TeamSummaryDto>>.Success(teams.Select(t => t.ToContract()).ToList());
    }
}
