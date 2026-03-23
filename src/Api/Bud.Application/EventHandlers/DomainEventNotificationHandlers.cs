
namespace Bud.Application.EventHandlers;

public sealed class GoalCreatedDomainEventNotifier(
    NotificationOrchestrator notificationOrchestrator) : IDomainEventNotifier<GoalCreatedDomainEvent>
{
    public async Task HandleAsync(
        GoalCreatedDomainEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        await notificationOrchestrator.NotifyGoalCreatedAsync(
            domainEvent.GoalId,
            domainEvent.OrganizationId,
            domainEvent.GoalName,
            domainEvent.ActorCollaboratorId,
            cancellationToken);
    }
}

public sealed class GoalUpdatedDomainEventNotifier(
    NotificationOrchestrator notificationOrchestrator) : IDomainEventNotifier<GoalUpdatedDomainEvent>
{
    public async Task HandleAsync(
        GoalUpdatedDomainEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        await notificationOrchestrator.NotifyGoalUpdatedAsync(
            domainEvent.GoalId,
            domainEvent.OrganizationId,
            domainEvent.GoalName,
            domainEvent.ActorCollaboratorId,
            cancellationToken);
    }
}

public sealed class GoalDeletedDomainEventNotifier(
    NotificationOrchestrator notificationOrchestrator) : IDomainEventNotifier<GoalDeletedDomainEvent>
{
    public async Task HandleAsync(
        GoalDeletedDomainEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        await notificationOrchestrator.NotifyGoalDeletedAsync(
            domainEvent.GoalId,
            domainEvent.OrganizationId,
            domainEvent.GoalName,
            domainEvent.ActorCollaboratorId,
            cancellationToken);
    }
}

public sealed class CheckinCreatedDomainEventNotifier(
    NotificationOrchestrator notificationOrchestrator) : IDomainEventNotifier<CheckinCreatedDomainEvent>
{
    public async Task HandleAsync(
        CheckinCreatedDomainEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        await notificationOrchestrator.NotifyCheckinCreatedAsync(
            domainEvent.CheckinId,
            domainEvent.IndicatorId,
            domainEvent.OrganizationId,
            domainEvent.CollaboratorId,
            domainEvent.IndicatorName,
            cancellationToken);
    }
}
