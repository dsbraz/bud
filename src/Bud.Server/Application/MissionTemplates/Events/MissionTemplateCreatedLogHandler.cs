using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.MissionTemplates.Events;

namespace Bud.Server.Application.MissionTemplates.Events;

public sealed partial class MissionTemplateCreatedLogHandler(ILogger<MissionTemplateCreatedLogHandler> logger) : IDomainEventSubscriber<MissionTemplateCreatedDomainEvent>
{
    public Task HandleAsync(MissionTemplateCreatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        LogMissionTemplateCreatedProcessed(logger, domainEvent.MissionTemplateId, domainEvent.OrganizationId);
        return Task.CompletedTask;
    }

    [LoggerMessage(
        EventId = 3700,
        Level = LogLevel.Information,
        Message = "Evento MissionTemplateCreated processado. MissionTemplateId={MissionTemplateId} OrganizationId={OrganizationId}")]
    private static partial void LogMissionTemplateCreatedProcessed(ILogger logger, Guid missionTemplateId, Guid organizationId);
}
