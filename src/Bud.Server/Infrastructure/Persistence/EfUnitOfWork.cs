using Bud.Server.Domain.Abstractions;

namespace Bud.Server.Infrastructure.Persistence;

public sealed class EfUnitOfWork(
    ApplicationDbContext dbContext,
    IDomainEventDispatcher? domainEventDispatcher = null) : IUnitOfWork
{
    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        var domainEventSources = dbContext.ChangeTracker
            .Entries()
            .Select(e => e.Entity)
            .OfType<IHasDomainEvents>()
            .Where(entity => entity.DomainEvents.Count > 0)
            .Distinct()
            .ToList();

        var domainEvents = domainEventSources
            .SelectMany(entity => entity.DomainEvents)
            .ToArray();

        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var domainEventSource in domainEventSources)
        {
            domainEventSource.ClearDomainEvents();
        }

        if (domainEventDispatcher is null || domainEvents.Length == 0)
        {
            return;
        }

        await domainEventDispatcher.DispatchAsync(domainEvents, cancellationToken);
    }
}
