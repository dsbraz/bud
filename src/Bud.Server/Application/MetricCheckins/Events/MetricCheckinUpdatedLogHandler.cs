using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.MetricCheckins.Events;
using Microsoft.Extensions.Logging;
using Bud.Server.Logging;

namespace Bud.Server.Application.MetricCheckins.Events;

public sealed class MetricCheckinUpdatedLogHandler(ILogger<MetricCheckinUpdatedLogHandler> logger) : IDomainEventSubscriber<MetricCheckinUpdatedDomainEvent>
{
    public Task HandleAsync(MetricCheckinUpdatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        logger.LogMetricCheckinUpdatedProcessed(domainEvent.MetricCheckinId, domainEvent.MissionMetricId, domainEvent.OrganizationId, domainEvent.CollaboratorId);
        return Task.CompletedTask;
    }
}
