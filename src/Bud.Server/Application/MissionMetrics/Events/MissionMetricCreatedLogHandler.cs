using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.MissionMetrics.Events;
using Microsoft.Extensions.Logging;

namespace Bud.Server.Application.MissionMetrics.Events;

public sealed partial class MissionMetricCreatedLogHandler(ILogger<MissionMetricCreatedLogHandler> logger) : IDomainEventSubscriber<MissionMetricCreatedDomainEvent>
{
    public Task HandleAsync(MissionMetricCreatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        LogMissionMetricCreatedProcessed(logger, domainEvent.MissionMetricId, domainEvent.MissionId, domainEvent.OrganizationId);
        return Task.CompletedTask;
    }

    [LoggerMessage(
        EventId = 3450,
        Level = LogLevel.Information,
        Message = "Evento MissionMetricCreated processado. MetricId={MetricId} MissionId={MissionId} OrganizationId={OrganizationId}")]
    private static partial void LogMissionMetricCreatedProcessed(ILogger logger, Guid metricId, Guid missionId, Guid organizationId);
}
