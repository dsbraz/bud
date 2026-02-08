using Bud.Shared.Contracts;
using Bud.Shared.Models;

namespace Bud.Server.Application.Abstractions;

public interface IOutboxAdministrationService
{
    Task<ServiceResult<PagedResult<OutboxDeadLetterDto>>> GetDeadLettersAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ServiceResult> ReprocessDeadLetterAsync(Guid messageId, CancellationToken cancellationToken = default);
    Task<ServiceResult<int>> ReprocessDeadLettersAsync(ReprocessDeadLettersRequest request, CancellationToken cancellationToken = default);
}
