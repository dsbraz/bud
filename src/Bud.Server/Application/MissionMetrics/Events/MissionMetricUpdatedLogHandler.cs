using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.MissionMetrics.Events;
using Microsoft.Extensions.Logging;

namespace Bud.Server.Application.MissionMetrics.Events;

public sealed partial class MissionMetricUpdatedLogHandler(ILogger<MissionMetricUpdatedLogHandler> logger) : IDomainEventSubscriber<MissionMetricUpdatedDomainEvent>
{
    public Task HandleAsync(MissionMetricUpdatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        LogMissionMetricUpdatedProcessed(logger, domainEvent.MissionMetricId, domainEvent.MissionId, domainEvent.OrganizationId);
        return Task.CompletedTask;
    }

    [LoggerMessage(
        EventId = 3451,
        Level = LogLevel.Information,
        Message = "Evento MissionMetricUpdated processado. MissionMetricId={MissionMetricId} MissionId={MissionId} OrganizationId={OrganizationId}")]
    private static partial void LogMissionMetricUpdatedProcessed(ILogger logger, Guid missionMetricId, Guid missionId, Guid organizationId);
}
