using Bud.Application.Common;

namespace Bud.Application.Ports;

public interface IAuthService
{
    Task<Result<LoginResult>> LoginAsync(CreateSessionRequest request, CancellationToken cancellationToken = default);
    Task<Result<List<OrganizationSnapshot>>> GetMyOrganizationsAsync(string email, CancellationToken cancellationToken = default);
}
