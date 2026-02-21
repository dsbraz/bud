using Bud.Server.Infrastructure.Repositories;
using Bud.Server.Infrastructure.Services;
using Bud.Shared.Domain;

namespace Bud.Server.Application.Notifications;

/// <summary>
/// Orquestra a criacao de notificacoes para eventos de dominio.
/// </summary>
public class NotificationOrchestrator(
    INotificationRepository notificationRepository,
    INotificationRecipientResolver notificationRecipientResolver)
{
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
        Guid missionMetricId,
        Guid organizationId,
        Guid? excludeCollaboratorId,
        CancellationToken cancellationToken = default)
    {
        var missionId = await notificationRecipientResolver.ResolveMissionIdFromMetricAsync(missionMetricId, cancellationToken);
        if (!missionId.HasValue)
        {
            return;
        }

        var recipients = await notificationRecipientResolver.ResolveMissionRecipientsAsync(
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
        var recipients = await notificationRecipientResolver.ResolveMissionRecipientsAsync(
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

        await notificationRepository.AddRangeAsync(notifications, cancellationToken);
        await notificationRepository.SaveChangesAsync(cancellationToken);
    }
}
