using Bud.Shared.Contracts;

namespace Bud.Server.Application.Auth;

public interface IAuthQueryUseCase
{
    Task<ServiceResult<List<OrganizationSummaryDto>>> GetMyOrganizationsAsync(string email, CancellationToken cancellationToken = default);
}
