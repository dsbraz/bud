using Bud.Server.Application.Common;

namespace Bud.Server.Application.Sessions;

public sealed class DeleteCurrentSession
{
    public Task<Result> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Success());
    }
}
