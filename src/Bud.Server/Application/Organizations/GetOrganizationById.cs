using Bud.Server.Application.Common;
using Bud.Server.Domain.Abstractions;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Server.Settings;
using Bud.Shared.Contracts;
using Microsoft.Extensions.Options;

namespace Bud.Server.Application.Organizations;

public sealed class GetOrganizationById(IOrganizationRepository organizationRepository)
{
    public async Task<Result<Organization>> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var organization = await organizationRepository.GetByIdAsync(id, cancellationToken);
        return organization is null
            ? Result<Organization>.NotFound("Organização não encontrada.")
            : Result<Organization>.Success(organization);
    }
}

