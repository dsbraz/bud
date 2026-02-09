using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.Organizations.Events;
using Microsoft.Extensions.Logging;
using Bud.Server.Logging;

namespace Bud.Server.Application.Organizations.Events;

public sealed class OrganizationCreatedLogHandler(ILogger<OrganizationCreatedLogHandler> logger) : IDomainEventSubscriber<OrganizationCreatedDomainEvent>
{
    public Task HandleAsync(OrganizationCreatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        logger.LogOrganizationCreatedProcessed(domainEvent.OrganizationId);
        return Task.CompletedTask;
    }
}
