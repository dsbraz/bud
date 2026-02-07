using Bud.Shared.Contracts;

namespace Bud.Server.Application.Auth;

public sealed class AuthCommandUseCase(IAuthService authService) : IAuthCommandUseCase
{
    public Task<ServiceResult<AuthLoginResponse>> LoginAsync(AuthLoginRequest request, CancellationToken cancellationToken = default)
        => authService.LoginAsync(request, cancellationToken);
}
