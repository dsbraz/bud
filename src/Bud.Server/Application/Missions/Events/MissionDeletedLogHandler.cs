using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.Missions.Events;
using Microsoft.Extensions.Logging;

namespace Bud.Server.Application.Missions.Events;

public sealed class MissionDeletedLogHandler(ILogger<MissionDeletedLogHandler> logger) : IDomainEventSubscriber<MissionDeletedDomainEvent>
{
    public Task HandleAsync(MissionDeletedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Evento MissionDeleted processado. MissionId={MissionId} OrganizationId={OrganizationId}", domainEvent.MissionId, domainEvent.OrganizationId);
        return Task.CompletedTask;
    }
}
