using Bud.Server.Application.Abstractions;
using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.MetricCheckins.Events;
using Bud.Shared.Models;

namespace Bud.Server.Application.Notifications.Events;

public sealed partial class MetricCheckinCreatedNotificationHandler(
    INotificationService notificationService,
    INotificationRecipientResolver recipientResolver,
    ILogger<MetricCheckinCreatedNotificationHandler> logger) : IDomainEventSubscriber<MetricCheckinCreatedDomainEvent>
{
    public async Task HandleAsync(MetricCheckinCreatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var missionId = await recipientResolver.ResolveMissionIdFromMetricAsync(
            domainEvent.MissionMetricId, cancellationToken);

        if (missionId is null)
        {
            LogMetricNotFound(logger, domainEvent.MissionMetricId);
            return;
        }

        var recipients = await recipientResolver.ResolveMissionRecipientsAsync(
            missionId.Value,
            domainEvent.OrganizationId,
            excludeCollaboratorId: domainEvent.CollaboratorId,
            cancellationToken: cancellationToken);

        if (recipients.Count == 0)
        {
            LogNoRecipients(logger, domainEvent.MetricCheckinId);
            return;
        }

        await notificationService.CreateForMultipleRecipientsAsync(
            recipients,
            domainEvent.OrganizationId,
            "Novo check-in registrado",
            "Um novo check-in de métrica foi registrado.",
            NotificationType.MetricCheckinCreated,
            domainEvent.MetricCheckinId,
            "MetricCheckin",
            cancellationToken);

        LogNotificationCreated(logger, domainEvent.MetricCheckinId, recipients.Count);
    }

    [LoggerMessage(
        EventId = 5230,
        Level = LogLevel.Information,
        Message = "Notificações criadas para check-in {MetricCheckinId}. Destinatários={RecipientCount}")]
    private static partial void LogNotificationCreated(ILogger logger, Guid metricCheckinId, int recipientCount);

    [LoggerMessage(
        EventId = 5231,
        Level = LogLevel.Debug,
        Message = "Nenhum destinatário encontrado para notificação do check-in {MetricCheckinId}")]
    private static partial void LogNoRecipients(ILogger logger, Guid metricCheckinId);

    [LoggerMessage(
        EventId = 5232,
        Level = LogLevel.Warning,
        Message = "Métrica {MissionMetricId} não encontrada para criação de notificação de check-in")]
    private static partial void LogMetricNotFound(ILogger logger, Guid missionMetricId);
}
