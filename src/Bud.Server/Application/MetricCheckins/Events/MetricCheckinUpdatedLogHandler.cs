using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.MetricCheckins.Events;
using Microsoft.Extensions.Logging;

namespace Bud.Server.Application.MetricCheckins.Events;

public sealed partial class MetricCheckinUpdatedLogHandler(ILogger<MetricCheckinUpdatedLogHandler> logger) : IDomainEventSubscriber<MetricCheckinUpdatedDomainEvent>
{
    public Task HandleAsync(MetricCheckinUpdatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        LogMetricCheckinUpdatedProcessed(logger, domainEvent.MetricCheckinId, domainEvent.MissionMetricId, domainEvent.OrganizationId, domainEvent.CollaboratorId);
        return Task.CompletedTask;
    }

    [LoggerMessage(
        EventId = 3461,
        Level = LogLevel.Information,
        Message = "Evento MetricCheckinUpdated processado. MetricCheckinId={MetricCheckinId} MissionMetricId={MissionMetricId} OrganizationId={OrganizationId} CollaboratorId={CollaboratorId}")]
    private static partial void LogMetricCheckinUpdatedProcessed(ILogger logger, Guid metricCheckinId, Guid missionMetricId, Guid organizationId, Guid collaboratorId);
}
