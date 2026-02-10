using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.MissionTemplates.Events;

namespace Bud.Server.Application.MissionTemplates.Events;

public sealed partial class MissionTemplateUpdatedLogHandler(ILogger<MissionTemplateUpdatedLogHandler> logger) : IDomainEventSubscriber<MissionTemplateUpdatedDomainEvent>
{
    public Task HandleAsync(MissionTemplateUpdatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        LogMissionTemplateUpdatedProcessed(logger, domainEvent.MissionTemplateId, domainEvent.OrganizationId);
        return Task.CompletedTask;
    }

    [LoggerMessage(
        EventId = 3701,
        Level = LogLevel.Information,
        Message = "Evento MissionTemplateUpdated processado. MissionTemplateId={MissionTemplateId} OrganizationId={OrganizationId}")]
    private static partial void LogMissionTemplateUpdatedProcessed(ILogger logger, Guid missionTemplateId, Guid organizationId);
}
