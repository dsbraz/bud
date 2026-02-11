using Bud.Server.Application.Abstractions;
using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.Missions.Events;
using Bud.Shared.Models;

namespace Bud.Server.Application.Notifications.Events;

public sealed partial class MissionUpdatedNotificationHandler(
    INotificationService notificationService,
    INotificationRecipientResolver recipientResolver,
    ILogger<MissionUpdatedNotificationHandler> logger) : IDomainEventSubscriber<MissionUpdatedDomainEvent>
{
    public async Task HandleAsync(MissionUpdatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
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
            "Missão atualizada",
            "Uma missão foi atualizada na sua organização.",
            NotificationType.MissionUpdated,
            domainEvent.MissionId,
            "Mission",
            cancellationToken);

        LogNotificationCreated(logger, domainEvent.MissionId, recipients.Count);
    }

    [LoggerMessage(
        EventId = 5210,
        Level = LogLevel.Information,
        Message = "Notificações criadas para atualização de missão {MissionId}. Destinatários={RecipientCount}")]
    private static partial void LogNotificationCreated(ILogger logger, Guid missionId, int recipientCount);

    [LoggerMessage(
        EventId = 5211,
        Level = LogLevel.Debug,
        Message = "Nenhum destinatário encontrado para notificação de atualização da missão {MissionId}")]
    private static partial void LogNoRecipients(ILogger logger, Guid missionId);
}
