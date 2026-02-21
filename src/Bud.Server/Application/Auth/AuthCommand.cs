using Bud.Server.Infrastructure.Services;
using Bud.Server.Application.Common;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.Auth;

public sealed class AuthCommand(IAuthService authService)
{
    public async Task<Result<AuthLoginResponse>> LoginAsync(AuthLoginRequest request, CancellationToken cancellationToken = default)
    {
        var result = await authService.LoginAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return Result<AuthLoginResponse>.Failure(result.Error ?? "Falha ao autenticar.", result.ErrorType);
        }

        return Result<AuthLoginResponse>.Success(result.Value!.ToContract());
    }
}
