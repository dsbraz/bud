namespace Bud.Server.Application.Common.Pipeline;

public sealed class NoOpUseCasePipeline : IUseCasePipeline
{
    public static readonly NoOpUseCasePipeline Instance = new();

    private NoOpUseCasePipeline()
    {
    }

    public Task<TResponse> ExecuteAsync<TResponse>(
        UseCaseExecutionContext context,
        Func<CancellationToken, Task<TResponse>> operation,
        CancellationToken cancellationToken = default)
        => operation(cancellationToken);
}
