using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.Missions.Events;
using Microsoft.Extensions.Logging;

namespace Bud.Server.Application.Missions.Events;

public sealed class MissionCreatedLogHandler(ILogger<MissionCreatedLogHandler> logger) : IDomainEventSubscriber<MissionCreatedDomainEvent>
{
    public Task HandleAsync(MissionCreatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Evento MissionCreated processado. MissionId={MissionId} OrganizationId={OrganizationId}",
            domainEvent.MissionId,
            domainEvent.OrganizationId);

        return Task.CompletedTask;
    }
}
