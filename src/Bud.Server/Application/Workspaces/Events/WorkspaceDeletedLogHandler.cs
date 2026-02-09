using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.Workspaces.Events;
using Microsoft.Extensions.Logging;

namespace Bud.Server.Application.Workspaces.Events;

public sealed partial class WorkspaceDeletedLogHandler(ILogger<WorkspaceDeletedLogHandler> logger) : IDomainEventSubscriber<WorkspaceDeletedDomainEvent>
{
    public Task HandleAsync(WorkspaceDeletedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        LogWorkspaceDeletedProcessed(logger, domainEvent.WorkspaceId, domainEvent.OrganizationId);
        return Task.CompletedTask;
    }

    [LoggerMessage(
        EventId = 3432,
        Level = LogLevel.Information,
        Message = "Evento WorkspaceDeleted processado. WorkspaceId={WorkspaceId} OrganizationId={OrganizationId}")]
    private static partial void LogWorkspaceDeletedProcessed(ILogger logger, Guid workspaceId, Guid organizationId);
}
