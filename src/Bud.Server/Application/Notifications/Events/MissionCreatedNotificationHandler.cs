using Bud.Server.Application.Abstractions;
using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.Missions.Events;
using Bud.Shared.Models;

namespace Bud.Server.Application.Notifications.Events;

public sealed partial class MissionCreatedNotificationHandler(
    INotificationService notificationService,
    INotificationRecipientResolver recipientResolver,
    ILogger<MissionCreatedNotificationHandler> logger) : IDomainEventSubscriber<MissionCreatedDomainEvent>
{
    public async Task HandleAsync(MissionCreatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
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
            "Nova missão criada",
            "Uma nova missão foi criada na sua organização.",
            NotificationType.MissionCreated,
            domainEvent.MissionId,
            "Mission",
            cancellationToken);

        LogNotificationCreated(logger, domainEvent.MissionId, recipients.Count);
    }

    [LoggerMessage(
        EventId = 5200,
        Level = LogLevel.Information,
        Message = "Notificações criadas para missão {MissionId}. Destinatários={RecipientCount}")]
    private static partial void LogNotificationCreated(ILogger logger, Guid missionId, int recipientCount);

    [LoggerMessage(
        EventId = 5201,
        Level = LogLevel.Debug,
        Message = "Nenhum destinatário encontrado para notificação da missão {MissionId}")]
    private static partial void LogNoRecipients(ILogger logger, Guid missionId);
}
