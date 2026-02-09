using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.Workspaces.Events;
using Microsoft.Extensions.Logging;

namespace Bud.Server.Application.Workspaces.Events;

public sealed partial class WorkspaceCreatedLogHandler(ILogger<WorkspaceCreatedLogHandler> logger) : IDomainEventSubscriber<WorkspaceCreatedDomainEvent>
{
    public Task HandleAsync(WorkspaceCreatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        LogWorkspaceCreatedProcessed(logger, domainEvent.WorkspaceId, domainEvent.OrganizationId);
        return Task.CompletedTask;
    }

    [LoggerMessage(
        EventId = 3430,
        Level = LogLevel.Information,
        Message = "Evento WorkspaceCreated processado. WorkspaceId={WorkspaceId} OrganizationId={OrganizationId}")]
    private static partial void LogWorkspaceCreatedProcessed(ILogger logger, Guid workspaceId, Guid organizationId);
}
