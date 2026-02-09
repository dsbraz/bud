using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.Organizations.Events;
using Microsoft.Extensions.Logging;
using Bud.Server.Logging;

namespace Bud.Server.Application.Organizations.Events;

public sealed class OrganizationDeletedLogHandler(ILogger<OrganizationDeletedLogHandler> logger) : IDomainEventSubscriber<OrganizationDeletedDomainEvent>
{
    public Task HandleAsync(OrganizationDeletedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        logger.LogOrganizationDeletedProcessed(domainEvent.OrganizationId);
        return Task.CompletedTask;
    }
}
