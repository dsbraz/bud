using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Domain.ReadModels;
using Bud.Server.Authorization;
using Bud.Server.Domain.Abstractions;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.UseCases.Teams;

public sealed class GetTeamById(ITeamRepository teamRepository)
{
    public async Task<Result<Team>> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var team = await teamRepository.GetByIdAsync(id, cancellationToken);
        return team is null
            ? Result<Team>.NotFound("Time não encontrado.")
            : Result<Team>.Success(team);
    }
}

