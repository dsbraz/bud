using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.MissionMetrics.Events;
using Microsoft.Extensions.Logging;

namespace Bud.Server.Application.MissionMetrics.Events;

public sealed partial class MissionMetricDeletedLogHandler(ILogger<MissionMetricDeletedLogHandler> logger) : IDomainEventSubscriber<MissionMetricDeletedDomainEvent>
{
    public Task HandleAsync(MissionMetricDeletedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        LogMissionMetricDeletedProcessed(logger, domainEvent.MissionMetricId, domainEvent.MissionId, domainEvent.OrganizationId);
        return Task.CompletedTask;
    }

    [LoggerMessage(
        EventId = 3452,
        Level = LogLevel.Information,
        Message = "Evento MissionMetricDeleted processado. MissionMetricId={MissionMetricId} MissionId={MissionId} OrganizationId={OrganizationId}")]
    private static partial void LogMissionMetricDeletedProcessed(ILogger logger, Guid missionMetricId, Guid missionId, Guid organizationId);
}
