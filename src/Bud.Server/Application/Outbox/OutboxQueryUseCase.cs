using Bud.Shared.Contracts;

namespace Bud.Server.Application.Outbox;

public sealed class OutboxQueryUseCase(IOutboxAdministrationService outboxAdministrationService) : IOutboxQueryUseCase
{
    public Task<ServiceResult<PagedResult<OutboxDeadLetterDto>>> GetDeadLettersAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        => outboxAdministrationService.GetDeadLettersAsync(page, pageSize, cancellationToken);
}
