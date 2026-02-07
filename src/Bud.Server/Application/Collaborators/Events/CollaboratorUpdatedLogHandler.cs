using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.Collaborators.Events;
using Microsoft.Extensions.Logging;

namespace Bud.Server.Application.Collaborators.Events;

public sealed class CollaboratorUpdatedLogHandler(ILogger<CollaboratorUpdatedLogHandler> logger) : IDomainEventSubscriber<CollaboratorUpdatedDomainEvent>
{
    public Task HandleAsync(CollaboratorUpdatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Evento CollaboratorUpdated processado. CollaboratorId={CollaboratorId} OrganizationId={OrganizationId}", domainEvent.CollaboratorId, domainEvent.OrganizationId);
        return Task.CompletedTask;
    }
}
