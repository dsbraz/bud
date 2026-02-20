using Bud.Server.Application.Ports;
using Bud.Shared.Domain;

namespace Bud.Server.Application.Notifications;

/// <summary>
/// Orquestra a criacao de notificacoes para eventos de dominio.
/// </summary>
public sealed class NotificationOrchestrator(
    INotificationRepository notificationRepository,
    INotificationRecipientResolver recipientResolver) : INotificationOrchestrator
{
    public async Task NotifyMissionCreatedAsync(
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

    public async Task NotifyMissionUpdatedAsync(
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

    public async Task NotifyMissionDeletedAsync(
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

    public async Task NotifyMetricCheckinCreatedAsync(
        Guid checkinId,
        Guid missionMetricId,
        Guid organizationId,
        Guid? excludeCollaboratorId,
        CancellationToken cancellationToken = default)
    {
        var missionId = await recipientResolver.ResolveMissionIdFromMetricAsync(missionMetricId, cancellationToken);
        if (!missionId.HasValue)
        {
            return;
        }

        var recipients = await recipientResolver.ResolveMissionRecipientsAsync(
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
        var recipients = await recipientResolver.ResolveMissionRecipientsAsync(
            missionId,
            organizationId,
            excludeCollaboratorId: null,
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
