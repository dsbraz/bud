using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.MetricCheckins.Events;

namespace Bud.Server.Application.MetricCheckins.Events;

public sealed partial class MetricCheckinDeletedLogHandler(ILogger<MetricCheckinDeletedLogHandler> logger) : IDomainEventSubscriber<MetricCheckinDeletedDomainEvent>
{
    public Task HandleAsync(MetricCheckinDeletedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        LogMetricCheckinDeletedProcessed(logger, domainEvent.MetricCheckinId, domainEvent.MissionMetricId, domainEvent.OrganizationId, domainEvent.CollaboratorId);
        return Task.CompletedTask;
    }

    [LoggerMessage(
        EventId = 3462,
        Level = LogLevel.Information,
        Message = "Evento MetricCheckinDeleted processado. MetricCheckinId={MetricCheckinId} MissionMetricId={MissionMetricId} OrganizationId={OrganizationId} CollaboratorId={CollaboratorId}")]
    private static partial void LogMetricCheckinDeletedProcessed(ILogger logger, Guid metricCheckinId, Guid missionMetricId, Guid organizationId, Guid collaboratorId);
}
