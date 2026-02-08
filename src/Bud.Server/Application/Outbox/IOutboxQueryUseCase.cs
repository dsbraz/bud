using Bud.Shared.Contracts;
using Bud.Shared.Models;

namespace Bud.Server.Application.Outbox;

public interface IOutboxQueryUseCase
{
    Task<ServiceResult<PagedResult<OutboxDeadLetterDto>>> GetDeadLettersAsync(int page, int pageSize, CancellationToken cancellationToken = default);
}
