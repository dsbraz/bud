namespace Bud.Server.Application.Common.Pipeline;

public sealed class UseCasePipeline(IEnumerable<IUseCaseBehavior> behaviors) : IUseCasePipeline
{
    private readonly List<IUseCaseBehavior> _behaviors = behaviors.ToList();

    public Task<TResponse> ExecuteAsync<TResponse>(
        UseCaseExecutionContext context,
        Func<CancellationToken, Task<TResponse>> operation,
        CancellationToken cancellationToken = default)
    {
        return ExecuteInternalAsync(0, context, operation, cancellationToken);
    }

    private Task<TResponse> ExecuteInternalAsync<TResponse>(
        int index,
        UseCaseExecutionContext context,
        Func<CancellationToken, Task<TResponse>> operation,
        CancellationToken cancellationToken)
    {
        if (index >= _behaviors.Count)
        {
            return operation(cancellationToken);
        }

        return _behaviors[index].HandleAsync(
            context,
            ct => ExecuteInternalAsync(index + 1, context, operation, ct),
            cancellationToken);
    }
}
