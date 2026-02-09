using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.Workspaces.Events;
using Microsoft.Extensions.Logging;
using Bud.Server.Logging;

namespace Bud.Server.Application.Workspaces.Events;

public sealed class WorkspaceCreatedLogHandler(ILogger<WorkspaceCreatedLogHandler> logger) : IDomainEventSubscriber<WorkspaceCreatedDomainEvent>
{
    public Task HandleAsync(WorkspaceCreatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        logger.LogWorkspaceCreatedProcessed(domainEvent.WorkspaceId, domainEvent.OrganizationId);
        return Task.CompletedTask;
    }
}
