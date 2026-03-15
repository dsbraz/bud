using Bud.Application.Common;
using Microsoft.Extensions.Logging;

namespace Bud.Application.Features.Sessions;

public sealed partial class DeleteCurrentSession(ILogger<DeleteCurrentSession> logger)
{
    public Task<Result> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        LogDeletingCurrentSession(logger);
        LogCurrentSessionDeleted(logger);
        return Task.FromResult(Result.Success());
    }

    [LoggerMessage(EventId = 4093, Level = LogLevel.Information, Message = "Deleting current session")]
    private static partial void LogDeletingCurrentSession(ILogger logger);

    [LoggerMessage(EventId = 4094, Level = LogLevel.Information, Message = "Current session deleted successfully")]
    private static partial void LogCurrentSessionDeleted(ILogger logger);
}
