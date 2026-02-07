using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.MissionMetrics.Events;
using Microsoft.Extensions.Logging;

namespace Bud.Server.Application.MissionMetrics.Events;

public sealed class MissionMetricCreatedLogHandler(ILogger<MissionMetricCreatedLogHandler> logger) : IDomainEventSubscriber<MissionMetricCreatedDomainEvent>
{
    public Task HandleAsync(MissionMetricCreatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Evento MissionMetricCreated processado. MetricId={MetricId} MissionId={MissionId} OrganizationId={OrganizationId}",
            domainEvent.MissionMetricId,
            domainEvent.MissionId,
            domainEvent.OrganizationId);

        return Task.CompletedTask;
    }
}
