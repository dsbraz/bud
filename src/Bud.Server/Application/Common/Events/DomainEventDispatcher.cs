using Bud.Server.Domain.Common.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Bud.Server.Application.Common.Events;

public sealed class DomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
{
    public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var handlerType = typeof(IDomainEventSubscriber<>).MakeGenericType(domainEvent.GetType());
        var handlers = serviceProvider.GetServices(handlerType);

        foreach (var handler in handlers)
        {
            var method = handlerType.GetMethod(nameof(IDomainEventSubscriber<IDomainEvent>.HandleAsync));
            if (method is null)
            {
                continue;
            }

            var task = (Task?)method.Invoke(handler, [domainEvent, cancellationToken]);
            if (task is not null)
            {
                await task;
            }
        }
    }
}
