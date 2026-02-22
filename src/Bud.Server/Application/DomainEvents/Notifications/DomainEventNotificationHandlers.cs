using Bud.Server.Domain.Abstractions;
using Bud.Server.Domain.Events;

namespace Bud.Server.Application.DomainEvents.Notifications;

public sealed class MissionCreatedDomainEventConsumer(
    NotificationOrchestrator notificationOrchestrator) : IDomainEventConsumer<MissionCreatedDomainEvent>
{
    public async Task HandleAsync(
        MissionCreatedDomainEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        await notificationOrchestrator.NotifyMissionCreatedAsync(
            domainEvent.MissionId,
            domainEvent.OrganizationId,
            cancellationToken);
    }
}

public sealed class MissionUpdatedDomainEventConsumer(
    NotificationOrchestrator notificationOrchestrator) : IDomainEventConsumer<MissionUpdatedDomainEvent>
{
    public async Task HandleAsync(
        MissionUpdatedDomainEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        await notificationOrchestrator.NotifyMissionUpdatedAsync(
            domainEvent.MissionId,
            domainEvent.OrganizationId,
            cancellationToken);
    }
}

public sealed class MissionDeletedDomainEventConsumer(
    NotificationOrchestrator notificationOrchestrator) : IDomainEventConsumer<MissionDeletedDomainEvent>
{
    public async Task HandleAsync(
        MissionDeletedDomainEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        await notificationOrchestrator.NotifyMissionDeletedAsync(
            domainEvent.MissionId,
            domainEvent.OrganizationId,
            cancellationToken);
    }
}

public sealed class MetricCheckinCreatedDomainEventConsumer(
    NotificationOrchestrator notificationOrchestrator) : IDomainEventConsumer<MetricCheckinCreatedDomainEvent>
{
    public async Task HandleAsync(
        MetricCheckinCreatedDomainEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        await notificationOrchestrator.NotifyMetricCheckinCreatedAsync(
            domainEvent.CheckinId,
            domainEvent.MetricId,
            domainEvent.OrganizationId,
            domainEvent.ExcludeCollaboratorId,
            cancellationToken);
    }
}
