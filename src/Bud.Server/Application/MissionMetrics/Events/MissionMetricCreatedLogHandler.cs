using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.MissionMetrics.Events;
using Microsoft.Extensions.Logging;
using Bud.Server.Logging;

namespace Bud.Server.Application.MissionMetrics.Events;

public sealed class MissionMetricCreatedLogHandler(ILogger<MissionMetricCreatedLogHandler> logger) : IDomainEventSubscriber<MissionMetricCreatedDomainEvent>
{
    public Task HandleAsync(MissionMetricCreatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        logger.LogMissionMetricCreatedProcessed(domainEvent.MissionMetricId, domainEvent.MissionId, domainEvent.OrganizationId);

        return Task.CompletedTask;
    }
}
