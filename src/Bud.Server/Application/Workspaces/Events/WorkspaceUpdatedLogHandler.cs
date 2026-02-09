using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.Workspaces.Events;
using Microsoft.Extensions.Logging;
using Bud.Server.Logging;

namespace Bud.Server.Application.Workspaces.Events;

public sealed class WorkspaceUpdatedLogHandler(ILogger<WorkspaceUpdatedLogHandler> logger) : IDomainEventSubscriber<WorkspaceUpdatedDomainEvent>
{
    public Task HandleAsync(WorkspaceUpdatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        logger.LogWorkspaceUpdatedProcessed(domainEvent.WorkspaceId, domainEvent.OrganizationId);
        return Task.CompletedTask;
    }
}
