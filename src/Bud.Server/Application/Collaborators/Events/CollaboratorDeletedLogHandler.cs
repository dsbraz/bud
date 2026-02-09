using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.Collaborators.Events;

namespace Bud.Server.Application.Collaborators.Events;

public sealed partial class CollaboratorDeletedLogHandler(ILogger<CollaboratorDeletedLogHandler> logger) : IDomainEventSubscriber<CollaboratorDeletedDomainEvent>
{
    public Task HandleAsync(CollaboratorDeletedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        LogCollaboratorDeletedProcessed(logger, domainEvent.CollaboratorId, domainEvent.OrganizationId);
        return Task.CompletedTask;
    }

    [LoggerMessage(
        EventId = 3442,
        Level = LogLevel.Information,
        Message = "Evento CollaboratorDeleted processado. CollaboratorId={CollaboratorId} OrganizationId={OrganizationId}")]
    private static partial void LogCollaboratorDeletedProcessed(ILogger logger, Guid collaboratorId, Guid organizationId);
}
