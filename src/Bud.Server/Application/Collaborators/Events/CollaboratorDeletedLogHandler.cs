using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.Collaborators.Events;
using Microsoft.Extensions.Logging;

namespace Bud.Server.Application.Collaborators.Events;

public sealed class CollaboratorDeletedLogHandler(ILogger<CollaboratorDeletedLogHandler> logger) : IDomainEventSubscriber<CollaboratorDeletedDomainEvent>
{
    public Task HandleAsync(CollaboratorDeletedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Evento CollaboratorDeleted processado. CollaboratorId={CollaboratorId} OrganizationId={OrganizationId}", domainEvent.CollaboratorId, domainEvent.OrganizationId);
        return Task.CompletedTask;
    }
}
