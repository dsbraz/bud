using Bud.Server.Application.Common;
using Bud.Server.Domain.Abstractions;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Server.Settings;
using Bud.Shared.Contracts;
using Microsoft.Extensions.Options;

namespace Bud.Server.Application.Organizations;

public sealed class ListOrganizationCollaborators(IOrganizationRepository organizationRepository)
{
    public async Task<Result<Bud.Shared.Contracts.PagedResult<Collaborator>>> ExecuteAsync(
        Guid id,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);

        if (!await organizationRepository.ExistsAsync(id, cancellationToken))
        {
            return Result<Bud.Shared.Contracts.PagedResult<Collaborator>>.NotFound("Organização não encontrada.");
        }

        var result = await organizationRepository.GetCollaboratorsAsync(id, page, pageSize, cancellationToken);
        return Result<Bud.Shared.Contracts.PagedResult<Collaborator>>.Success(result.MapPaged(x => x));
    }
}
