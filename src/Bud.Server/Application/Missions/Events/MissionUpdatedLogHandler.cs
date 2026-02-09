using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.Missions.Events;
using Microsoft.Extensions.Logging;
using Bud.Server.Logging;

namespace Bud.Server.Application.Missions.Events;

public sealed class MissionUpdatedLogHandler(ILogger<MissionUpdatedLogHandler> logger) : IDomainEventSubscriber<MissionUpdatedDomainEvent>
{
    public Task HandleAsync(MissionUpdatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        logger.LogMissionUpdatedProcessed(domainEvent.MissionId, domainEvent.OrganizationId);
        return Task.CompletedTask;
    }
}
