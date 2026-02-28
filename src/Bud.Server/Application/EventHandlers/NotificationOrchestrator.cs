using Bud.Server.Application.Common;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Server.Application.Ports;

namespace Bud.Server.Application.EventHandlers;

/// <summary>
/// Coordinates notification creation for domain events.
/// </summary>
public class NotificationOrchestrator
{
    private readonly INotificationRepository _notificationRepository;
    private readonly INotificationRecipientResolver _notificationRecipientResolver;
    private readonly IUnitOfWork? _unitOfWork;

    public NotificationOrchestrator(
        INotificationRepository notificationRepository,
        INotificationRecipientResolver notificationRecipientResolver)
        : this(notificationRepository, notificationRecipientResolver, null)
    {
    }

    public NotificationOrchestrator(
        INotificationRepository notificationRepository,
        INotificationRecipientResolver notificationRecipientResolver,
        IUnitOfWork? unitOfWork)
    {
        _notificationRepository = notificationRepository;
        _notificationRecipientResolver = notificationRecipientResolver;
        _unitOfWork = unitOfWork;
    }

    public virtual async Task NotifyGoalCreatedAsync(
        Guid goalId,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        await NotifyGoalEventAsync(
            goalId,
            organizationId,
            "Nova meta criada",
            "Uma nova meta foi criada na sua organização.",
            NotificationType.GoalCreated,
            cancellationToken);
    }

    public virtual async Task NotifyGoalUpdatedAsync(
        Guid goalId,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        await NotifyGoalEventAsync(
            goalId,
            organizationId,
            "Meta atualizada",
            "Uma meta foi atualizada na sua organização.",
            NotificationType.GoalUpdated,
            cancellationToken);
    }

    public virtual async Task NotifyGoalDeletedAsync(
        Guid goalId,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        await NotifyGoalEventAsync(
            goalId,
            organizationId,
            "Meta removida",
            "Uma meta foi removida da sua organização.",
            NotificationType.GoalDeleted,
            cancellationToken);
    }

    public virtual async Task NotifyCheckinCreatedAsync(
        Guid checkinId,
        Guid indicatorId,
        Guid organizationId,
        Guid? excludeCollaboratorId,
        CancellationToken cancellationToken = default)
    {
        var goalId = await _notificationRecipientResolver.ResolveGoalIdFromIndicatorAsync(indicatorId, cancellationToken);
        if (!goalId.HasValue)
        {
            return;
        }

        var recipients = await _notificationRecipientResolver.ResolveGoalRecipientsAsync(
            goalId.Value,
            organizationId,
            excludeCollaboratorId,
            cancellationToken);

        if (recipients.Count == 0)
        {
            return;
        }

        await CreateNotificationsAsync(
            recipients,
            organizationId,
            "Novo check-in registrado",
            "Um novo check-in de indicador foi registrado.",
            NotificationType.CheckinCreated,
            checkinId,
            "Checkin",
            cancellationToken);
    }

    private async Task NotifyGoalEventAsync(
        Guid goalId,
        Guid organizationId,
        string title,
        string message,
        NotificationType type,
        CancellationToken cancellationToken)
    {
        var recipients = await _notificationRecipientResolver.ResolveGoalRecipientsAsync(
            goalId,
            organizationId,
            null,
            cancellationToken);

        if (recipients.Count == 0)
        {
            return;
        }

        await CreateNotificationsAsync(
            recipients,
            organizationId,
            title,
            message,
            type,
            goalId,
            "Goal",
            cancellationToken);
    }

    private async Task CreateNotificationsAsync(
        IEnumerable<Guid> recipientIds,
        Guid organizationId,
        string title,
        string message,
        NotificationType type,
        Guid? relatedEntityId,
        string? relatedEntityType,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var notifications = recipientIds.Select(recipientId => Notification.Create(
            Guid.NewGuid(),
            recipientId,
            organizationId,
            title,
            message,
            type,
            now,
            relatedEntityId,
            relatedEntityType)).ToList();

        if (notifications.Count == 0)
        {
            return;
        }

        await _notificationRepository.AddRangeAsync(notifications, cancellationToken);
        await _unitOfWork.CommitAsync(_notificationRepository.SaveChangesAsync, cancellationToken);
    }
}
