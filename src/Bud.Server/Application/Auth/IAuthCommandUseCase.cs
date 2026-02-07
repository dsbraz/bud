using Bud.Shared.Contracts;

namespace Bud.Server.Application.Auth;

public interface IAuthCommandUseCase
{
    Task<ServiceResult<AuthLoginResponse>> LoginAsync(AuthLoginRequest request, CancellationToken cancellationToken = default);
}
