using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Domain.ReadModels;
using Bud.Server.Authorization;
using Bud.Server.Domain.Abstractions;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Server.Domain.ValueObjects;
using Bud.Server.MultiTenancy;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.Collaborators;

public sealed class ListCollaborators(ICollaboratorRepository collaboratorRepository)
{
    public async Task<Result<Bud.Shared.Contracts.PagedResult<Collaborator>>> ExecuteAsync(
        Guid? teamId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);
        var result = await collaboratorRepository.GetAllAsync(teamId, search, page, pageSize, cancellationToken);
        return Result<Bud.Shared.Contracts.PagedResult<Collaborator>>.Success(result.MapPaged(x => x));
    }
}

