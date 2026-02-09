using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.Collaborators.Events;

namespace Bud.Server.Application.Collaborators.Events;

public sealed partial class CollaboratorUpdatedLogHandler(ILogger<CollaboratorUpdatedLogHandler> logger) : IDomainEventSubscriber<CollaboratorUpdatedDomainEvent>
{
    public Task HandleAsync(CollaboratorUpdatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        LogCollaboratorUpdatedProcessed(logger, domainEvent.CollaboratorId, domainEvent.OrganizationId);
        return Task.CompletedTask;
    }

    [LoggerMessage(
        EventId = 3441,
        Level = LogLevel.Information,
        Message = "Evento CollaboratorUpdated processado. CollaboratorId={CollaboratorId} OrganizationId={OrganizationId}")]
    private static partial void LogCollaboratorUpdatedProcessed(ILogger logger, Guid collaboratorId, Guid organizationId);
}
