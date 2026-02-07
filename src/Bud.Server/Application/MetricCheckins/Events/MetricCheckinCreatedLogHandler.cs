using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.MetricCheckins.Events;
using Microsoft.Extensions.Logging;

namespace Bud.Server.Application.MetricCheckins.Events;

public sealed class MetricCheckinCreatedLogHandler(ILogger<MetricCheckinCreatedLogHandler> logger) : IDomainEventSubscriber<MetricCheckinCreatedDomainEvent>
{
    public Task HandleAsync(MetricCheckinCreatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Evento MetricCheckinCreated processado. CheckinId={CheckinId} MetricId={MetricId} OrganizationId={OrganizationId} CollaboratorId={CollaboratorId}",
            domainEvent.MetricCheckinId,
            domainEvent.MissionMetricId,
            domainEvent.OrganizationId,
            domainEvent.CollaboratorId);
        return Task.CompletedTask;
    }
}
