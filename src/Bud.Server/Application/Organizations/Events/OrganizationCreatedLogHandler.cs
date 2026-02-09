using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.Organizations.Events;
using Microsoft.Extensions.Logging;

namespace Bud.Server.Application.Organizations.Events;

public sealed partial class OrganizationCreatedLogHandler(ILogger<OrganizationCreatedLogHandler> logger) : IDomainEventSubscriber<OrganizationCreatedDomainEvent>
{
    public Task HandleAsync(OrganizationCreatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        LogOrganizationCreatedProcessed(logger, domainEvent.OrganizationId);
        return Task.CompletedTask;
    }

    [LoggerMessage(
        EventId = 3410,
        Level = LogLevel.Information,
        Message = "Evento OrganizationCreated processado. OrganizationId={OrganizationId}")]
    private static partial void LogOrganizationCreatedProcessed(ILogger logger, Guid organizationId);
}
