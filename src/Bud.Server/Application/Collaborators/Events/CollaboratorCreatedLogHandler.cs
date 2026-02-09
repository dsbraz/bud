using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.Collaborators.Events;

namespace Bud.Server.Application.Collaborators.Events;

public sealed partial class CollaboratorCreatedLogHandler(ILogger<CollaboratorCreatedLogHandler> logger) : IDomainEventSubscriber<CollaboratorCreatedDomainEvent>
{
    public Task HandleAsync(CollaboratorCreatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        LogCollaboratorCreatedProcessed(logger, domainEvent.CollaboratorId, domainEvent.OrganizationId);
        return Task.CompletedTask;
    }

    [LoggerMessage(
        EventId = 3440,
        Level = LogLevel.Information,
        Message = "Evento CollaboratorCreated processado. CollaboratorId={CollaboratorId} OrganizationId={OrganizationId}")]
    private static partial void LogCollaboratorCreatedProcessed(ILogger logger, Guid collaboratorId, Guid organizationId);
}
