using System.Security.Claims;
using Bud.Application.Common;
using Bud.Application.Mapping;
using Bud.Application.Ports;
using Bud.Domain.Model;
using Bud.Domain.Repositories;
using Bud.Shared.Contracts;

namespace Bud.Application.UseCases.Teams;

public sealed class ListTeamCollaborators(ITeamRepository teamRepository)
{
    public async Task<Result<Bud.Shared.Contracts.Common.PagedResult<Collaborator>>> ExecuteAsync(
        Guid id,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);

        if (!await teamRepository.ExistsAsync(id, cancellationToken))
        {
            return Result<Bud.Shared.Contracts.Common.PagedResult<Collaborator>>.NotFound(UserErrorMessages.TeamNotFound);
        }

        var result = await teamRepository.GetCollaboratorsAsync(id, page, pageSize, cancellationToken);
        return Result<Bud.Shared.Contracts.Common.PagedResult<Collaborator>>.Success(result.MapPaged(x => x));
    }
}

