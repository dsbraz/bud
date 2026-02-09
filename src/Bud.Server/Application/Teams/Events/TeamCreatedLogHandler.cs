using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.Teams.Events;
using Microsoft.Extensions.Logging;

namespace Bud.Server.Application.Teams.Events;

public sealed partial class TeamCreatedLogHandler(ILogger<TeamCreatedLogHandler> logger) : IDomainEventSubscriber<TeamCreatedDomainEvent>
{
    public Task HandleAsync(TeamCreatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        LogTeamCreatedProcessed(logger, domainEvent.TeamId, domainEvent.OrganizationId, domainEvent.WorkspaceId);
        return Task.CompletedTask;
    }

    [LoggerMessage(
        EventId = 3420,
        Level = LogLevel.Information,
        Message = "Evento TeamCreated processado. TeamId={TeamId} OrganizationId={OrganizationId} WorkspaceId={WorkspaceId}")]
    private static partial void LogTeamCreatedProcessed(ILogger logger, Guid teamId, Guid organizationId, Guid workspaceId);
}
