using Bud.Server.Application.Common.Pipeline;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.Outbox;

public sealed class OutboxCommandUseCase(
    IOutboxAdministrationService outboxAdministrationService,
    IUseCasePipeline? useCasePipeline = null) : IOutboxCommandUseCase
{
    private readonly IUseCasePipeline _useCasePipeline = useCasePipeline ?? NoOpUseCasePipeline.Instance;

    public async Task<ServiceResult> ReprocessDeadLetterAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        return await _useCasePipeline.ExecuteAsync(
            new UseCaseExecutionContext(nameof(OutboxCommandUseCase), nameof(ReprocessDeadLetterAsync)),
            ct => outboxAdministrationService.ReprocessDeadLetterAsync(messageId, ct),
            cancellationToken);
    }

    public async Task<ServiceResult<int>> ReprocessDeadLettersAsync(ReprocessDeadLettersRequest request, CancellationToken cancellationToken = default)
    {
        return await _useCasePipeline.ExecuteAsync(
            new UseCaseExecutionContext(nameof(OutboxCommandUseCase), nameof(ReprocessDeadLettersAsync)),
            ct => outboxAdministrationService.ReprocessDeadLettersAsync(request, ct),
            cancellationToken);
    }
}
