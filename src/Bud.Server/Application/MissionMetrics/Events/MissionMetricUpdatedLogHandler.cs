using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.MissionMetrics.Events;
using Microsoft.Extensions.Logging;

namespace Bud.Server.Application.MissionMetrics.Events;

public sealed class MissionMetricUpdatedLogHandler(ILogger<MissionMetricUpdatedLogHandler> logger) : IDomainEventSubscriber<MissionMetricUpdatedDomainEvent>
{
    public Task HandleAsync(MissionMetricUpdatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Evento MissionMetricUpdated processado. MissionMetricId={MissionMetricId} MissionId={MissionId} OrganizationId={OrganizationId}", domainEvent.MissionMetricId, domainEvent.MissionId, domainEvent.OrganizationId);
        return Task.CompletedTask;
    }
}
