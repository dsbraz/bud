using Bud.Server.Application.Common;
using Bud.Server.Infrastructure.Services;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.Auth;

public sealed class AuthenticateCollaborator(IAuthService authService)
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
