using Bud.Server.Domain.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Bud.Server.Infrastructure.DomainEvents;

public sealed class DomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
{
    public async Task DispatchAsync(
        IReadOnlyCollection<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default)
    {
        if (domainEvents.Count == 0)
        {
            return;
        }

        foreach (var domainEvent in domainEvents)
        {
            var handlerType = typeof(IDomainEventConsumer<>).MakeGenericType(domainEvent.GetType());
            var handlers = serviceProvider.GetServices(handlerType);
            var handleMethod = handlerType.GetMethod(nameof(IDomainEventConsumer<IDomainEvent>.HandleAsync))
                ?? throw new InvalidOperationException($"Método HandleAsync não encontrado para {handlerType.Name}.");

            foreach (var handler in handlers)
            {
                if (handler is null)
                {
                    continue;
                }

                var task = handleMethod.Invoke(handler, [domainEvent, cancellationToken]) as Task;
                if (task is null)
                {
                    throw new InvalidOperationException($"Handler {handler.GetType().Name} retornou resultado inválido.");
                }

                await task;
            }
        }
    }
}
