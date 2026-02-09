using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.Organizations.Events;

namespace Bud.Server.Application.Organizations.Events;

public sealed partial class OrganizationDeletedLogHandler(ILogger<OrganizationDeletedLogHandler> logger) : IDomainEventSubscriber<OrganizationDeletedDomainEvent>
{
    public Task HandleAsync(OrganizationDeletedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        LogOrganizationDeletedProcessed(logger, domainEvent.OrganizationId);
        return Task.CompletedTask;
    }

    [LoggerMessage(
        EventId = 3412,
        Level = LogLevel.Information,
        Message = "Evento OrganizationDeleted processado. OrganizationId={OrganizationId}")]
    private static partial void LogOrganizationDeletedProcessed(ILogger logger, Guid organizationId);
}
