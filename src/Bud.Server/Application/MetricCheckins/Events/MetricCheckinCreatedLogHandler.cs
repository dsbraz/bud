using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.MetricCheckins.Events;
using Microsoft.Extensions.Logging;
using Bud.Server.Logging;

namespace Bud.Server.Application.MetricCheckins.Events;

public sealed class MetricCheckinCreatedLogHandler(ILogger<MetricCheckinCreatedLogHandler> logger) : IDomainEventSubscriber<MetricCheckinCreatedDomainEvent>
{
    public Task HandleAsync(MetricCheckinCreatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        logger.LogMetricCheckinCreatedProcessed(domainEvent.MetricCheckinId, domainEvent.MissionMetricId, domainEvent.OrganizationId, domainEvent.CollaboratorId);
        return Task.CompletedTask;
    }
}
