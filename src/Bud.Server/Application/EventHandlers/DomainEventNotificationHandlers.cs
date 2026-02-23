using Bud.Server.Domain.Events;

namespace Bud.Server.Application.EventHandlers;

public sealed class MissionCreatedDomainEventNotifier(
    NotificationOrchestrator notificationOrchestrator) : IDomainEventNotifier<MissionCreatedDomainEvent>
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

public sealed class MissionUpdatedDomainEventNotifier(
    NotificationOrchestrator notificationOrchestrator) : IDomainEventNotifier<MissionUpdatedDomainEvent>
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

public sealed class MissionDeletedDomainEventNotifier(
    NotificationOrchestrator notificationOrchestrator) : IDomainEventNotifier<MissionDeletedDomainEvent>
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

public sealed class MetricCheckinCreatedDomainEventNotifier(
    NotificationOrchestrator notificationOrchestrator) : IDomainEventNotifier<MetricCheckinCreatedDomainEvent>
{
    public async Task HandleAsync(
        MetricCheckinCreatedDomainEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        await notificationOrchestrator.NotifyMetricCheckinCreatedAsync(
            domainEvent.CheckinId,
            domainEvent.MetricId,
            domainEvent.OrganizationId,
            domainEvent.CreatorCollaboratorId,
            cancellationToken);
    }
}
