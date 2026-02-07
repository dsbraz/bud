using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.Organizations.Events;
using Microsoft.Extensions.Logging;

namespace Bud.Server.Application.Organizations.Events;

public sealed class OrganizationDeletedLogHandler(ILogger<OrganizationDeletedLogHandler> logger) : IDomainEventSubscriber<OrganizationDeletedDomainEvent>
{
    public Task HandleAsync(OrganizationDeletedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Evento OrganizationDeleted processado. OrganizationId={OrganizationId}", domainEvent.OrganizationId);
        return Task.CompletedTask;
    }
}
