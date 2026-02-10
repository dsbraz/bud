using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.MissionTemplates.Events;

namespace Bud.Server.Application.MissionTemplates.Events;

public sealed partial class MissionTemplateDeletedLogHandler(ILogger<MissionTemplateDeletedLogHandler> logger) : IDomainEventSubscriber<MissionTemplateDeletedDomainEvent>
{
    public Task HandleAsync(MissionTemplateDeletedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        LogMissionTemplateDeletedProcessed(logger, domainEvent.MissionTemplateId, domainEvent.OrganizationId);
        return Task.CompletedTask;
    }

    [LoggerMessage(
        EventId = 3702,
        Level = LogLevel.Information,
        Message = "Evento MissionTemplateDeleted processado. MissionTemplateId={MissionTemplateId} OrganizationId={OrganizationId}")]
    private static partial void LogMissionTemplateDeletedProcessed(ILogger logger, Guid missionTemplateId, Guid organizationId);
}
