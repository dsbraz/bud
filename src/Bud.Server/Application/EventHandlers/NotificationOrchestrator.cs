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
        string goalName,
        Guid? actorCollaboratorId,
        CancellationToken cancellationToken = default)
    {
        var actorName = actorCollaboratorId.HasValue
            ? await _notificationRecipientResolver.ResolveCollaboratorNameAsync(actorCollaboratorId.Value, cancellationToken)
            : null;

        var message = actorName is not null
            ? $"{actorName} criou a meta '{goalName}'"
            : $"Uma nova meta foi criada: '{goalName}'";

        await NotifyGoalEventAsync(goalId, organizationId, "Nova meta criada", message, NotificationType.GoalCreated, cancellationToken);
    }

    public virtual async Task NotifyGoalUpdatedAsync(
        Guid goalId,
        Guid organizationId,
        string goalName,
        Guid? actorCollaboratorId,
        CancellationToken cancellationToken = default)
    {
        var actorName = actorCollaboratorId.HasValue
            ? await _notificationRecipientResolver.ResolveCollaboratorNameAsync(actorCollaboratorId.Value, cancellationToken)
            : null;

        var message = actorName is not null
            ? $"{actorName} atualizou a meta '{goalName}'"
            : $"A meta '{goalName}' foi atualizada";

        await NotifyGoalEventAsync(goalId, organizationId, "Meta atualizada", message, NotificationType.GoalUpdated, cancellationToken);
    }

    public virtual async Task NotifyGoalDeletedAsync(
        Guid goalId,
        Guid organizationId,
        string goalName,
        Guid? actorCollaboratorId,
        CancellationToken cancellationToken = default)
    {
        var actorName = actorCollaboratorId.HasValue
            ? await _notificationRecipientResolver.ResolveCollaboratorNameAsync(actorCollaboratorId.Value, cancellationToken)
            : null;

        var message = actorName is not null
            ? $"{actorName} removeu a meta '{goalName}'"
            : $"A meta '{goalName}' foi removida";

        await NotifyGoalEventAsync(goalId, organizationId, "Meta removida", message, NotificationType.GoalDeleted, cancellationToken);
    }

    public virtual async Task NotifyCheckinCreatedAsync(
        Guid checkinId,
        Guid indicatorId,
        Guid organizationId,
        Guid? excludeCollaboratorId,
        string indicatorName,
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

        var actorName = excludeCollaboratorId.HasValue
            ? await _notificationRecipientResolver.ResolveCollaboratorNameAsync(excludeCollaboratorId.Value, cancellationToken)
            : null;

        var message = actorName is not null
            ? $"{actorName} registrou um check-in no indicador '{indicatorName}'"
            : $"Um novo check-in foi registrado no indicador '{indicatorName}'";

        await CreateNotificationsAsync(
            recipients,
            organizationId,
            "Novo check-in registrado",
            message,
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
