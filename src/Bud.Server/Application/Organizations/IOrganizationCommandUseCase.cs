using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using Bud.Server.Application.Common;

namespace Bud.Server.Application.Organizations;

public interface IOrganizationCommandUseCase
{
    Task<Result<Organization>> CreateAsync(CreateOrganizationRequest request, CancellationToken cancellationToken = default);
    Task<Result<Organization>> UpdateAsync(Guid id, UpdateOrganizationRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
