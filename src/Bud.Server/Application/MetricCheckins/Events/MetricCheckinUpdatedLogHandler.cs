using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.MetricCheckins.Events;
using Microsoft.Extensions.Logging;

namespace Bud.Server.Application.MetricCheckins.Events;

public sealed class MetricCheckinUpdatedLogHandler(ILogger<MetricCheckinUpdatedLogHandler> logger) : IDomainEventSubscriber<MetricCheckinUpdatedDomainEvent>
{
    public Task HandleAsync(MetricCheckinUpdatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Evento MetricCheckinUpdated processado. MetricCheckinId={MetricCheckinId} MissionMetricId={MissionMetricId} OrganizationId={OrganizationId} CollaboratorId={CollaboratorId}", domainEvent.MetricCheckinId, domainEvent.MissionMetricId, domainEvent.OrganizationId, domainEvent.CollaboratorId);
        return Task.CompletedTask;
    }
}
