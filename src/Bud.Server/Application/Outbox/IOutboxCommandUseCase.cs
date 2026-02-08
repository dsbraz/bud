using Bud.Shared.Contracts;

namespace Bud.Server.Application.Outbox;

public interface IOutboxCommandUseCase
{
    Task<ServiceResult> ReprocessDeadLetterAsync(Guid messageId, CancellationToken cancellationToken = default);
    Task<ServiceResult<int>> ReprocessDeadLettersAsync(ReprocessDeadLettersRequest request, CancellationToken cancellationToken = default);
}
