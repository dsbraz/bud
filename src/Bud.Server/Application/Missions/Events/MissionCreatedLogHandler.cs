using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.Missions.Events;

namespace Bud.Server.Application.Missions.Events;

public sealed partial class MissionCreatedLogHandler(ILogger<MissionCreatedLogHandler> logger) : IDomainEventSubscriber<MissionCreatedDomainEvent>
{
    public Task HandleAsync(MissionCreatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        LogMissionCreatedProcessed(logger, domainEvent.MissionId, domainEvent.OrganizationId);
        return Task.CompletedTask;
    }

    [LoggerMessage(
        EventId = 3400,
        Level = LogLevel.Information,
        Message = "Evento MissionCreated processado. MissionId={MissionId} OrganizationId={OrganizationId}")]
    private static partial void LogMissionCreatedProcessed(ILogger logger, Guid missionId, Guid organizationId);
}
