using Bud.Server.Domain.ReadModels;
using Bud.Shared.Contracts;

namespace Bud.Server.Services;

public interface IAuthService
{
    Task<ServiceResult<AuthLoginResult>> LoginAsync(AuthLoginRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<List<OrganizationSummary>>> GetMyOrganizationsAsync(string email, CancellationToken cancellationToken = default);
}
