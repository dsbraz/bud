using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.Teams.Events;
using Microsoft.Extensions.Logging;

namespace Bud.Server.Application.Teams.Events;

public sealed class TeamUpdatedLogHandler(ILogger<TeamUpdatedLogHandler> logger) : IDomainEventSubscriber<TeamUpdatedDomainEvent>
{
    public Task HandleAsync(TeamUpdatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Evento TeamUpdated processado. TeamId={TeamId} OrganizationId={OrganizationId} WorkspaceId={WorkspaceId}", domainEvent.TeamId, domainEvent.OrganizationId, domainEvent.WorkspaceId);
        return Task.CompletedTask;
    }
}
