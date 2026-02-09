using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.MetricCheckins.Events;
using Microsoft.Extensions.Logging;

namespace Bud.Server.Application.MetricCheckins.Events;

public sealed partial class MetricCheckinCreatedLogHandler(ILogger<MetricCheckinCreatedLogHandler> logger) : IDomainEventSubscriber<MetricCheckinCreatedDomainEvent>
{
    public Task HandleAsync(MetricCheckinCreatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        LogMetricCheckinCreatedProcessed(logger, domainEvent.MetricCheckinId, domainEvent.MissionMetricId, domainEvent.OrganizationId, domainEvent.CollaboratorId);
        return Task.CompletedTask;
    }

    [LoggerMessage(
        EventId = 3460,
        Level = LogLevel.Information,
        Message = "Evento MetricCheckinCreated processado. CheckinId={CheckinId} MetricId={MetricId} OrganizationId={OrganizationId} CollaboratorId={CollaboratorId}")]
    private static partial void LogMetricCheckinCreatedProcessed(ILogger logger, Guid checkinId, Guid metricId, Guid organizationId, Guid collaboratorId);
}
