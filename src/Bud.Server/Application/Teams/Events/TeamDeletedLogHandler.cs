using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.Teams.Events;

namespace Bud.Server.Application.Teams.Events;

public sealed partial class TeamDeletedLogHandler(ILogger<TeamDeletedLogHandler> logger) : IDomainEventSubscriber<TeamDeletedDomainEvent>
{
    public Task HandleAsync(TeamDeletedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        LogTeamDeletedProcessed(logger, domainEvent.TeamId, domainEvent.OrganizationId, domainEvent.WorkspaceId);
        return Task.CompletedTask;
    }

    [LoggerMessage(
        EventId = 3422,
        Level = LogLevel.Information,
        Message = "Evento TeamDeleted processado. TeamId={TeamId} OrganizationId={OrganizationId} WorkspaceId={WorkspaceId}")]
    private static partial void LogTeamDeletedProcessed(ILogger logger, Guid teamId, Guid organizationId, Guid workspaceId);
}
