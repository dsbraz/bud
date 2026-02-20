using Bud.Server.Application.Common;
using Bud.Server.Services;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.Auth;

public sealed class AuthCommandUseCase(IAuthService authService) : IAuthCommandUseCase
{
    public async Task<ServiceResult<AuthLoginResponse>> LoginAsync(AuthLoginRequest request, CancellationToken cancellationToken = default)
    {
        var result = await authService.LoginAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return ServiceResult<AuthLoginResponse>.Failure(result.Error ?? "Falha ao autenticar.", result.ErrorType);
        }

        return ServiceResult<AuthLoginResponse>.Success(result.Value!.ToContract());
    }
}
