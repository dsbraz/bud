using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.Workspaces.Events;

namespace Bud.Server.Application.Workspaces.Events;

public sealed partial class WorkspaceUpdatedLogHandler(ILogger<WorkspaceUpdatedLogHandler> logger) : IDomainEventSubscriber<WorkspaceUpdatedDomainEvent>
{
    public Task HandleAsync(WorkspaceUpdatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        LogWorkspaceUpdatedProcessed(logger, domainEvent.WorkspaceId, domainEvent.OrganizationId);
        return Task.CompletedTask;
    }

    [LoggerMessage(
        EventId = 3431,
        Level = LogLevel.Information,
        Message = "Evento WorkspaceUpdated processado. WorkspaceId={WorkspaceId} OrganizationId={OrganizationId}")]
    private static partial void LogWorkspaceUpdatedProcessed(ILogger logger, Guid workspaceId, Guid organizationId);
}
