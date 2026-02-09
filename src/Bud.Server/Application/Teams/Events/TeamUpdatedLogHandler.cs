using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.Teams.Events;
using Microsoft.Extensions.Logging;
using Bud.Server.Logging;

namespace Bud.Server.Application.Teams.Events;

public sealed class TeamUpdatedLogHandler(ILogger<TeamUpdatedLogHandler> logger) : IDomainEventSubscriber<TeamUpdatedDomainEvent>
{
    public Task HandleAsync(TeamUpdatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        logger.LogTeamUpdatedProcessed(domainEvent.TeamId, domainEvent.OrganizationId, domainEvent.WorkspaceId);
        return Task.CompletedTask;
    }
}
