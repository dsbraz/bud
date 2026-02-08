using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.Collaborators.Events;
using Microsoft.Extensions.Logging;

namespace Bud.Server.Application.Collaborators.Events;

public sealed class CollaboratorCreatedLogHandler(ILogger<CollaboratorCreatedLogHandler> logger) : IDomainEventSubscriber<CollaboratorCreatedDomainEvent>
{
    public Task HandleAsync(CollaboratorCreatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Evento CollaboratorCreated processado. CollaboratorId={CollaboratorId} OrganizationId={OrganizationId}",
            domainEvent.CollaboratorId,
            domainEvent.OrganizationId);
        return Task.CompletedTask;
    }
}
