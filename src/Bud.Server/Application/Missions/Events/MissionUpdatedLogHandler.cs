using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.Missions.Events;
using Microsoft.Extensions.Logging;

namespace Bud.Server.Application.Missions.Events;

public sealed partial class MissionUpdatedLogHandler(ILogger<MissionUpdatedLogHandler> logger) : IDomainEventSubscriber<MissionUpdatedDomainEvent>
{
    public Task HandleAsync(MissionUpdatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        LogMissionUpdatedProcessed(logger, domainEvent.MissionId, domainEvent.OrganizationId);
        return Task.CompletedTask;
    }

    [LoggerMessage(
        EventId = 3401,
        Level = LogLevel.Information,
        Message = "Evento MissionUpdated processado. MissionId={MissionId} OrganizationId={OrganizationId}")]
    private static partial void LogMissionUpdatedProcessed(ILogger logger, Guid missionId, Guid organizationId);
}
