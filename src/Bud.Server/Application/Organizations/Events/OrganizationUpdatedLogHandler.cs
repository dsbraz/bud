using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.Organizations.Events;
using Microsoft.Extensions.Logging;
using Bud.Server.Logging;

namespace Bud.Server.Application.Organizations.Events;

public sealed class OrganizationUpdatedLogHandler(ILogger<OrganizationUpdatedLogHandler> logger) : IDomainEventSubscriber<OrganizationUpdatedDomainEvent>
{
    public Task HandleAsync(OrganizationUpdatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        logger.LogOrganizationUpdatedProcessed(domainEvent.OrganizationId);
        return Task.CompletedTask;
    }
}
