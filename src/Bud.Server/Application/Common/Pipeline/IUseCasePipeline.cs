namespace Bud.Server.Application.Common.Pipeline;

public interface IUseCasePipeline
{
    Task<TResponse> ExecuteAsync<TResponse>(
        UseCaseExecutionContext context,
        Func<CancellationToken, Task<TResponse>> operation,
        CancellationToken cancellationToken = default);
}
