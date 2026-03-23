using System.Security.Claims;
using Bud.Application.Common;
using Bud.Application.Ports;
using Bud.Shared.Contracts;

namespace Bud.Application.Features.Collaborators.UseCases;

public sealed class ListCollaborators(ICollaboratorRepository collaboratorRepository)
{
    public async Task<Result<PagedResult<Collaborator>>> ExecuteAsync(
        Guid? teamId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);
        var result = await collaboratorRepository.GetAllAsync(teamId, search, page, pageSize, cancellationToken);
        return Result<PagedResult<Collaborator>>.Success(result.MapPaged(x => x));
    }
}

