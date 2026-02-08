using Bud.Server.Domain.Common.Events;

namespace Bud.Server.Application.Common.Events;

public sealed class NoOpDomainEventDispatcher : IDomainEventDispatcher
{
    public static readonly NoOpDomainEventDispatcher Instance = new();

    private NoOpDomainEventDispatcher()
    {
    }

    public Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
