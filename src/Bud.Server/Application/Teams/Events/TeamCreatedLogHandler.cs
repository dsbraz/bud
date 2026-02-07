using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.Teams.Events;
using Microsoft.Extensions.Logging;

namespace Bud.Server.Application.Teams.Events;

public sealed class TeamCreatedLogHandler(ILogger<TeamCreatedLogHandler> logger) : IDomainEventSubscriber<TeamCreatedDomainEvent>
{
    public Task HandleAsync(TeamCreatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Evento TeamCreated processado. TeamId={TeamId} OrganizationId={OrganizationId} WorkspaceId={WorkspaceId}",
            domainEvent.TeamId,
            domainEvent.OrganizationId,
            domainEvent.WorkspaceId);
        return Task.CompletedTask;
    }
}
