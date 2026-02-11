using Bud.Server.Application.Abstractions;
using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.Missions.Events;
using Bud.Shared.Models;

namespace Bud.Server.Application.Notifications.Events;

public sealed partial class MissionDeletedNotificationHandler(
    INotificationService notificationService,
    INotificationRecipientResolver recipientResolver,
    ILogger<MissionDeletedNotificationHandler> logger) : IDomainEventSubscriber<MissionDeletedDomainEvent>
{
    public async Task HandleAsync(MissionDeletedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var recipients = await recipientResolver.ResolveMissionRecipientsAsync(
            domainEvent.MissionId,
            domainEvent.OrganizationId,
            cancellationToken: cancellationToken);

        if (recipients.Count == 0)
        {
            LogNoRecipients(logger, domainEvent.MissionId);
            return;
        }

        await notificationService.CreateForMultipleRecipientsAsync(
            recipients,
            domainEvent.OrganizationId,
            "Missão removida",
            "Uma missão foi removida da sua organização.",
            NotificationType.MissionDeleted,
            domainEvent.MissionId,
            "Mission",
            cancellationToken);

        LogNotificationCreated(logger, domainEvent.MissionId, recipients.Count);
    }

    [LoggerMessage(
        EventId = 5220,
        Level = LogLevel.Information,
        Message = "Notificações criadas para remoção de missão {MissionId}. Destinatários={RecipientCount}")]
    private static partial void LogNotificationCreated(ILogger logger, Guid missionId, int recipientCount);

    [LoggerMessage(
        EventId = 5221,
        Level = LogLevel.Debug,
        Message = "Nenhum destinatário encontrado para notificação de remoção da missão {MissionId}")]
    private static partial void LogNoRecipients(ILogger logger, Guid missionId);
}
