using Bud.Server.Application.Common;
using Bud.Server.Application.Ports;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.Sessions;

public sealed class CreateSession(IAuthService authService)
{
    public async Task<Result<AuthLoginResponse>> ExecuteAsync(
        AuthLoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await authService.LoginAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return Result<AuthLoginResponse>.Failure(result.Error ?? "Falha ao autenticar.", result.ErrorType);
        }

        return Result<AuthLoginResponse>.Success(result.Value!.ToContract());
    }
}
