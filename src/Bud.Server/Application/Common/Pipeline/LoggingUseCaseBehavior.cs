using Microsoft.Extensions.Logging;
using Bud.Server.Logging;

namespace Bud.Server.Application.Common.Pipeline;

public sealed class LoggingUseCaseBehavior(ILogger<LoggingUseCaseBehavior> logger) : IUseCaseBehavior
{
    public async Task<TResponse> HandleAsync<TResponse>(
        UseCaseExecutionContext context,
        Func<CancellationToken, Task<TResponse>> next,
        CancellationToken cancellationToken = default)
    {
        logger.LogUseCaseExecuting(context.UseCaseName, context.OperationName);
        try
        {
            var response = await next(cancellationToken);
            logger.LogUseCaseCompleted(context.UseCaseName, context.OperationName);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogUseCaseFailed(ex, context.UseCaseName, context.OperationName);
            throw;
        }
    }
}
