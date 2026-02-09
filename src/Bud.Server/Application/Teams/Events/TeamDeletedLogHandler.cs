using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.Teams.Events;
using Microsoft.Extensions.Logging;
using Bud.Server.Logging;

namespace Bud.Server.Application.Teams.Events;

public sealed class TeamDeletedLogHandler(ILogger<TeamDeletedLogHandler> logger) : IDomainEventSubscriber<TeamDeletedDomainEvent>
{
    public Task HandleAsync(TeamDeletedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        logger.LogTeamDeletedProcessed(domainEvent.TeamId, domainEvent.OrganizationId, domainEvent.WorkspaceId);
        return Task.CompletedTask;
    }
}
