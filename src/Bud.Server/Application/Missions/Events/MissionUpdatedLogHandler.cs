using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.Missions.Events;
using Microsoft.Extensions.Logging;

namespace Bud.Server.Application.Missions.Events;

public sealed class MissionUpdatedLogHandler(ILogger<MissionUpdatedLogHandler> logger) : IDomainEventSubscriber<MissionUpdatedDomainEvent>
{
    public Task HandleAsync(MissionUpdatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Evento MissionUpdated processado. MissionId={MissionId} OrganizationId={OrganizationId}", domainEvent.MissionId, domainEvent.OrganizationId);
        return Task.CompletedTask;
    }
}
