using Bud.Application.Common;

namespace Bud.Application.Features.Sessions;

public interface ISessionAuthenticator
{
    Task<Result<LoginResult>> LoginAsync(CreateSessionRequest request, CancellationToken cancellationToken = default);
}
