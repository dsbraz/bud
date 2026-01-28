using Bud.Shared.Contracts;

namespace Bud.Server.Services;

public interface IAuthService
{
    Task<ServiceResult<AuthLoginResponse>> LoginAsync(AuthLoginRequest request, CancellationToken cancellationToken = default);
}
