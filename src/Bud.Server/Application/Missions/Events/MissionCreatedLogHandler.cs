using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.Missions.Events;
using Microsoft.Extensions.Logging;
using Bud.Server.Logging;

namespace Bud.Server.Application.Missions.Events;

public sealed class MissionCreatedLogHandler(ILogger<MissionCreatedLogHandler> logger) : IDomainEventSubscriber<MissionCreatedDomainEvent>
{
    public Task HandleAsync(MissionCreatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        logger.LogMissionCreatedProcessed(domainEvent.MissionId, domainEvent.OrganizationId);

        return Task.CompletedTask;
    }
}
