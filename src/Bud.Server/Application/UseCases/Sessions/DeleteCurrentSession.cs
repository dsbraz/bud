using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;

namespace Bud.Server.Application.UseCases.Sessions;

public sealed class DeleteCurrentSession
{
    public Task<Result> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Success());
    }
}
