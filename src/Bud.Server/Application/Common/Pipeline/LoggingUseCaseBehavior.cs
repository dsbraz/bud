using Microsoft.Extensions.Logging;

namespace Bud.Server.Application.Common.Pipeline;

public sealed partial class LoggingUseCaseBehavior(ILogger<LoggingUseCaseBehavior> logger) : IUseCaseBehavior
{
    public async Task<TResponse> HandleAsync<TResponse>(
        UseCaseExecutionContext context,
        Func<CancellationToken, Task<TResponse>> next,
        CancellationToken cancellationToken = default)
    {
        LogUseCaseExecuting(logger, context.UseCaseName, context.OperationName);
        try
        {
            var response = await next(cancellationToken);
            LogUseCaseCompleted(logger, context.UseCaseName, context.OperationName);
            return response;
        }
        catch (Exception ex)
        {
            LogUseCaseFailed(logger, ex, context.UseCaseName, context.OperationName);
            throw;
        }
    }

    [LoggerMessage(
        EventId = 3300,
        Level = LogLevel.Information,
        Message = "Executando use case {UseCaseName}.{OperationName}")]
    private static partial void LogUseCaseExecuting(ILogger logger, string useCaseName, string operationName);

    [LoggerMessage(
        EventId = 3301,
        Level = LogLevel.Information,
        Message = "Use case {UseCaseName}.{OperationName} concluído")]
    private static partial void LogUseCaseCompleted(ILogger logger, string useCaseName, string operationName);

    [LoggerMessage(
        EventId = 3302,
        Level = LogLevel.Error,
        Message = "Erro no use case {UseCaseName}.{OperationName}")]
    private static partial void LogUseCaseFailed(ILogger logger, Exception exception, string useCaseName, string operationName);
}
