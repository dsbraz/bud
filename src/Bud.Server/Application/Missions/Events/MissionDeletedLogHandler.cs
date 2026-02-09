using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.Missions.Events;
using Microsoft.Extensions.Logging;
using Bud.Server.Logging;

namespace Bud.Server.Application.Missions.Events;

public sealed class MissionDeletedLogHandler(ILogger<MissionDeletedLogHandler> logger) : IDomainEventSubscriber<MissionDeletedDomainEvent>
{
    public Task HandleAsync(MissionDeletedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        logger.LogMissionDeletedProcessed(domainEvent.MissionId, domainEvent.OrganizationId);
        return Task.CompletedTask;
    }
}
