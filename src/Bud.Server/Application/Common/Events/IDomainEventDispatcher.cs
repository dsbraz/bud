using Bud.Server.Domain.Common.Events;

namespace Bud.Server.Application.Common.Events;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
}
