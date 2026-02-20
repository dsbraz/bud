using Bud.Server.Domain.ReadModels;
using Bud.Shared.Contracts;
using Bud.Server.Application.Common;

namespace Bud.Server.Application.Ports;

public interface IAuthService
{
    Task<Result<AuthLoginResult>> LoginAsync(AuthLoginRequest request, CancellationToken cancellationToken = default);
    Task<Result<List<OrganizationSummary>>> GetMyOrganizationsAsync(string email, CancellationToken cancellationToken = default);
}
