using Bud.Application.Common;
using Bud.Shared.Contracts;

namespace Bud.Application.Features.Organizations;

public sealed class ListOrganizationCollaborators(IOrganizationRepository organizationRepository)
{
    public async Task<Result<PagedResult<Collaborator>>> ExecuteAsync(
        Guid id,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);

        if (!await organizationRepository.ExistsAsync(id, cancellationToken))
        {
            return Result<PagedResult<Collaborator>>.NotFound(UserErrorMessages.OrganizationNotFound);
        }

        var result = await organizationRepository.GetCollaboratorsAsync(id, page, pageSize, cancellationToken);
        return Result<PagedResult<Collaborator>>.Success(result.MapPaged(x => x));
    }
}
