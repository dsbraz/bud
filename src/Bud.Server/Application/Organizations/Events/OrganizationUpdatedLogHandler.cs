using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.Organizations.Events;

namespace Bud.Server.Application.Organizations.Events;

public sealed partial class OrganizationUpdatedLogHandler(ILogger<OrganizationUpdatedLogHandler> logger) : IDomainEventSubscriber<OrganizationUpdatedDomainEvent>
{
    public Task HandleAsync(OrganizationUpdatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        LogOrganizationUpdatedProcessed(logger, domainEvent.OrganizationId);
        return Task.CompletedTask;
    }

    [LoggerMessage(
        EventId = 3411,
        Level = LogLevel.Information,
        Message = "Evento OrganizationUpdated processado. OrganizationId={OrganizationId}")]
    private static partial void LogOrganizationUpdatedProcessed(ILogger logger, Guid organizationId);
}
