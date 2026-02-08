using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.Organizations.Events;
using Microsoft.Extensions.Logging;

namespace Bud.Server.Application.Organizations.Events;

public sealed class OrganizationUpdatedLogHandler(ILogger<OrganizationUpdatedLogHandler> logger) : IDomainEventSubscriber<OrganizationUpdatedDomainEvent>
{
    public Task HandleAsync(OrganizationUpdatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Evento OrganizationUpdated processado. OrganizationId={OrganizationId}", domainEvent.OrganizationId);
        return Task.CompletedTask;
    }
}
