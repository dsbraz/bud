using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.Organizations.Events;
using Microsoft.Extensions.Logging;

namespace Bud.Server.Application.Organizations.Events;

public sealed class OrganizationCreatedLogHandler(ILogger<OrganizationCreatedLogHandler> logger) : IDomainEventSubscriber<OrganizationCreatedDomainEvent>
{
    public Task HandleAsync(OrganizationCreatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Evento OrganizationCreated processado. OrganizationId={OrganizationId}", domainEvent.OrganizationId);
        return Task.CompletedTask;
    }
}
