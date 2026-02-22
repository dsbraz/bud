using Bud.Server.Application.Common;
using Bud.Server.Domain.Abstractions;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Server.Application.Ports;

namespace Bud.Server.Application.DomainEvents.Notifications;

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

    public virtual async Task NotifyMissionCreatedAsync(
        Guid missionId,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        await NotifyMissionEventAsync(
            missionId,
            organizationId,
            "Nova missão criada",
            "Uma nova missão foi criada na sua organização.",
            NotificationType.MissionCreated,
            cancellationToken);
    }

    public virtual async Task NotifyMissionUpdatedAsync(
        Guid missionId,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        await NotifyMissionEventAsync(
            missionId,
            organizationId,
            "Missão atualizada",
            "Uma missão foi atualizada na sua organização.",
            NotificationType.MissionUpdated,
            cancellationToken);
    }

    public virtual async Task NotifyMissionDeletedAsync(
        Guid missionId,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        await NotifyMissionEventAsync(
            missionId,
            organizationId,
            "Missão removida",
            "Uma missão foi removida da sua organização.",
            NotificationType.MissionDeleted,
            cancellationToken);
    }

    public virtual async Task NotifyMetricCheckinCreatedAsync(
        Guid checkinId,
        Guid metricId,
        Guid organizationId,
        Guid? excludeCollaboratorId,
        CancellationToken cancellationToken = default)
    {
        var missionId = await _notificationRecipientResolver.ResolveMissionIdFromMetricAsync(metricId, cancellationToken);
        if (!missionId.HasValue)
        {
            return;
        }

        var recipients = await _notificationRecipientResolver.ResolveMissionRecipientsAsync(
            missionId.Value,
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
            "Um novo check-in de métrica foi registrado.",
            NotificationType.MetricCheckinCreated,
            checkinId,
            "MetricCheckin",
            cancellationToken);
    }

    private async Task NotifyMissionEventAsync(
        Guid missionId,
        Guid organizationId,
        string title,
        string message,
        NotificationType type,
        CancellationToken cancellationToken)
    {
        var recipients = await _notificationRecipientResolver.ResolveMissionRecipientsAsync(
            missionId,
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
            missionId,
            "Mission",
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
