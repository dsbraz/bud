using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.Missions.Events;

namespace Bud.Server.Application.Missions.Events;

public sealed partial class MissionDeletedLogHandler(ILogger<MissionDeletedLogHandler> logger) : IDomainEventSubscriber<MissionDeletedDomainEvent>
{
    public Task HandleAsync(MissionDeletedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        LogMissionDeletedProcessed(logger, domainEvent.MissionId, domainEvent.OrganizationId);
        return Task.CompletedTask;
    }

    [LoggerMessage(
        EventId = 3402,
        Level = LogLevel.Information,
        Message = "Evento MissionDeleted processado. MissionId={MissionId} OrganizationId={OrganizationId}")]
    private static partial void LogMissionDeletedProcessed(ILogger logger, Guid missionId, Guid organizationId);
}
