using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.MissionMetrics.Events;
using Microsoft.Extensions.Logging;

namespace Bud.Server.Application.MissionMetrics.Events;

public sealed class MissionMetricDeletedLogHandler(ILogger<MissionMetricDeletedLogHandler> logger) : IDomainEventSubscriber<MissionMetricDeletedDomainEvent>
{
    public Task HandleAsync(MissionMetricDeletedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Evento MissionMetricDeleted processado. MissionMetricId={MissionMetricId} MissionId={MissionId} OrganizationId={OrganizationId}", domainEvent.MissionMetricId, domainEvent.MissionId, domainEvent.OrganizationId);
        return Task.CompletedTask;
    }
}
