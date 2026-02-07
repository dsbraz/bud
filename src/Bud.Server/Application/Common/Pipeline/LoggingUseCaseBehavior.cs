using Microsoft.Extensions.Logging;

namespace Bud.Server.Application.Common.Pipeline;

public sealed class LoggingUseCaseBehavior(ILogger<LoggingUseCaseBehavior> logger) : IUseCaseBehavior
{
    public async Task<TResponse> HandleAsync<TResponse>(
        UseCaseExecutionContext context,
        Func<CancellationToken, Task<TResponse>> next,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Executando use case {UseCase}.{Operation}", context.UseCaseName, context.OperationName);
        try
        {
            var response = await next(cancellationToken);
            logger.LogInformation("Use case {UseCase}.{Operation} concluído", context.UseCaseName, context.OperationName);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro no use case {UseCase}.{Operation}", context.UseCaseName, context.OperationName);
            throw;
        }
    }
}
