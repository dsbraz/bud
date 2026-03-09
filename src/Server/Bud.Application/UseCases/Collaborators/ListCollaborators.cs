using System.Security.Claims;
using Bud.Application.Common;
using Bud.Application.Mapping;
using Bud.Application.Ports;
using Bud.Domain.Model;
using Bud.Domain.Repositories;
using Bud.Shared.Contracts;

namespace Bud.Application.UseCases.Collaborators;

public sealed class ListCollaborators(ICollaboratorRepository collaboratorRepository)
{
    public async Task<Result<Bud.Shared.Contracts.Common.PagedResult<Collaborator>>> ExecuteAsync(
        Guid? teamId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);
        var result = await collaboratorRepository.GetAllAsync(teamId, search, page, pageSize, cancellationToken);
        return Result<Bud.Shared.Contracts.Common.PagedResult<Collaborator>>.Success(result.MapPaged(x => x));
    }
}

