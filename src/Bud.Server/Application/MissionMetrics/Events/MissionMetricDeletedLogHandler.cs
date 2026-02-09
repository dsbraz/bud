using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.MissionMetrics.Events;
using Microsoft.Extensions.Logging;
using Bud.Server.Logging;

namespace Bud.Server.Application.MissionMetrics.Events;

public sealed class MissionMetricDeletedLogHandler(ILogger<MissionMetricDeletedLogHandler> logger) : IDomainEventSubscriber<MissionMetricDeletedDomainEvent>
{
    public Task HandleAsync(MissionMetricDeletedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        logger.LogMissionMetricDeletedProcessed(domainEvent.MissionMetricId, domainEvent.MissionId, domainEvent.OrganizationId);
        return Task.CompletedTask;
    }
}
