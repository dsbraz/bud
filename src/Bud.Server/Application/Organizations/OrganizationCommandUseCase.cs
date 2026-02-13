using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Application.Organizations;

public sealed class OrganizationCommandUseCase(
    IOrganizationService organizationService) : IOrganizationCommandUseCase
{
    public async Task<ServiceResult<Organization>> CreateAsync(CreateOrganizationRequest request, CancellationToken cancellationToken = default)
    {
        return await organizationService.CreateAsync(request, cancellationToken);
    }

    public async Task<ServiceResult<Organization>> UpdateAsync(Guid id, UpdateOrganizationRequest request, CancellationToken cancellationToken = default)
    {
        return await organizationService.UpdateAsync(id, request, cancellationToken);
    }

    public async Task<ServiceResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await organizationService.DeleteAsync(id, cancellationToken);
    }
}
