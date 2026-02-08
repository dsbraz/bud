using Bud.Shared.Contracts;
using Bud.Shared.Models;

namespace Bud.Server.Application.Organizations;

public interface IOrganizationCommandUseCase
{
    Task<ServiceResult<Organization>> CreateAsync(CreateOrganizationRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<Organization>> UpdateAsync(Guid id, UpdateOrganizationRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
