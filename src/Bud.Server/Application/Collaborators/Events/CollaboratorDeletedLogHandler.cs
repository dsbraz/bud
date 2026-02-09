using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.Collaborators.Events;
using Microsoft.Extensions.Logging;
using Bud.Server.Logging;

namespace Bud.Server.Application.Collaborators.Events;

public sealed class CollaboratorDeletedLogHandler(ILogger<CollaboratorDeletedLogHandler> logger) : IDomainEventSubscriber<CollaboratorDeletedDomainEvent>
{
    public Task HandleAsync(CollaboratorDeletedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        logger.LogCollaboratorDeletedProcessed(domainEvent.CollaboratorId, domainEvent.OrganizationId);
        return Task.CompletedTask;
    }
}
