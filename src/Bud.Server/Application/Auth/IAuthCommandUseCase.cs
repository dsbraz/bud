using Bud.Shared.Contracts;
using Bud.Server.Application.Common;

namespace Bud.Server.Application.Auth;

public interface IAuthCommandUseCase
{
    Task<Result<AuthLoginResponse>> LoginAsync(AuthLoginRequest request, CancellationToken cancellationToken = default);
}
