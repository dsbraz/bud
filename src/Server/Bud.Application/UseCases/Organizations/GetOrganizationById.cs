using Bud.Application.Common;
using Bud.Application.Mapping;
using Bud.Domain.Model;
using Bud.Domain.Repositories;
using Bud.Shared.Contracts;

namespace Bud.Application.UseCases.Organizations;

public sealed class GetOrganizationById(IOrganizationRepository organizationRepository)
{
    public async Task<Result<Organization>> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var organization = await organizationRepository.GetByIdAsync(id, cancellationToken);
        return organization is null
            ? Result<Organization>.NotFound(UserErrorMessages.OrganizationNotFound)
            : Result<Organization>.Success(organization);
    }
}
