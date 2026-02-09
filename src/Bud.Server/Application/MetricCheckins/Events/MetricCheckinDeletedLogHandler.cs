using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.MetricCheckins.Events;
using Microsoft.Extensions.Logging;
using Bud.Server.Logging;

namespace Bud.Server.Application.MetricCheckins.Events;

public sealed class MetricCheckinDeletedLogHandler(ILogger<MetricCheckinDeletedLogHandler> logger) : IDomainEventSubscriber<MetricCheckinDeletedDomainEvent>
{
    public Task HandleAsync(MetricCheckinDeletedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        logger.LogMetricCheckinDeletedProcessed(domainEvent.MetricCheckinId, domainEvent.MissionMetricId, domainEvent.OrganizationId, domainEvent.CollaboratorId);
        return Task.CompletedTask;
    }
}
