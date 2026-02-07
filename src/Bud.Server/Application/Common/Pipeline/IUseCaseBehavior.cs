namespace Bud.Server.Application.Common.Pipeline;

public interface IUseCaseBehavior
{
    Task<TResponse> HandleAsync<TResponse>(
        UseCaseExecutionContext context,
        Func<CancellationToken, Task<TResponse>> next,
        CancellationToken cancellationToken = default);
}
