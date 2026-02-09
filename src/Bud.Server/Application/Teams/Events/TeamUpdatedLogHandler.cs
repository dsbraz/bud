using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.Teams.Events;

namespace Bud.Server.Application.Teams.Events;

public sealed partial class TeamUpdatedLogHandler(ILogger<TeamUpdatedLogHandler> logger) : IDomainEventSubscriber<TeamUpdatedDomainEvent>
{
    public Task HandleAsync(TeamUpdatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        LogTeamUpdatedProcessed(logger, domainEvent.TeamId, domainEvent.OrganizationId, domainEvent.WorkspaceId);
        return Task.CompletedTask;
    }

    [LoggerMessage(
        EventId = 3421,
        Level = LogLevel.Information,
        Message = "Evento TeamUpdated processado. TeamId={TeamId} OrganizationId={OrganizationId} WorkspaceId={WorkspaceId}")]
    private static partial void LogTeamUpdatedProcessed(ILogger logger, Guid teamId, Guid organizationId, Guid workspaceId);
}
