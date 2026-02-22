using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Application.Ports;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.UseCases.Sessions;

public sealed class CreateSession(IAuthService authService)
{
    public async Task<Result<SessionResponse>> ExecuteAsync(
        CreateSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await authService.LoginAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return Result<SessionResponse>.Failure(result.Error ?? "Falha ao autenticar.", result.ErrorType);
        }

        return Result<SessionResponse>.Success(result.Value!.ToResponse());
    }
}
