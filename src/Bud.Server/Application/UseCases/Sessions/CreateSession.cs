using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Application.Ports;
using Bud.Shared.Contracts;
using Microsoft.Extensions.Logging;

namespace Bud.Server.Application.UseCases.Sessions;

public sealed partial class CreateSession(
    IAuthService authService,
    ILogger<CreateSession> logger)
{
    public async Task<Result<SessionResponse>> ExecuteAsync(
        CreateSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        LogCreatingSession(logger);

        var result = await authService.LoginAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            LogSessionCreationFailed(logger, result.Error ?? "Unknown error");
            return Result<SessionResponse>.Failure(result.Error ?? "Falha ao autenticar.", result.ErrorType);
        }

        LogSessionCreated(logger);
        return Result<SessionResponse>.Success(result.Value!.ToResponse());
    }

    [LoggerMessage(EventId = 4090, Level = LogLevel.Information, Message = "Creating session")]
    private static partial void LogCreatingSession(ILogger logger);

    [LoggerMessage(EventId = 4091, Level = LogLevel.Information, Message = "Session created successfully")]
    private static partial void LogSessionCreated(ILogger logger);

    [LoggerMessage(EventId = 4092, Level = LogLevel.Warning, Message = "Session creation failed: {Reason}")]
    private static partial void LogSessionCreationFailed(ILogger logger, string reason);
}
