using System.Security.Claims;
using Bud.Application.Common;
using Bud.Application.Mapping;
using Bud.Application.Ports;
using Bud.Domain.Model;
using Bud.Domain.Repositories;
using Bud.Shared.Contracts;

namespace Bud.Application.UseCases.Teams;

public sealed class GetTeamById(ITeamRepository teamRepository)
{
    public async Task<Result<Team>> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var team = await teamRepository.GetByIdAsync(id, cancellationToken);
        return team is null
            ? Result<Team>.NotFound(UserErrorMessages.TeamNotFound)
            : Result<Team>.Success(team);
    }
}

