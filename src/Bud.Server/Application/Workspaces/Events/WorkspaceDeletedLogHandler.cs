using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.Workspaces.Events;
using Microsoft.Extensions.Logging;

namespace Bud.Server.Application.Workspaces.Events;

public sealed class WorkspaceDeletedLogHandler(ILogger<WorkspaceDeletedLogHandler> logger) : IDomainEventSubscriber<WorkspaceDeletedDomainEvent>
{
    public Task HandleAsync(WorkspaceDeletedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Evento WorkspaceDeleted processado. WorkspaceId={WorkspaceId} OrganizationId={OrganizationId}", domainEvent.WorkspaceId, domainEvent.OrganizationId);
        return Task.CompletedTask;
    }
}
