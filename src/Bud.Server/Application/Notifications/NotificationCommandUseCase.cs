using Bud.Server.Infrastructure.Repositories;
using Bud.Server.Application.Common;
using Bud.Server.MultiTenancy;
using Bud.Shared.Domain;

namespace Bud.Server.Application.Notifications;

public sealed class NotificationCommandUseCase(
    INotificationRepository notificationRepository,
    ITenantProvider tenantProvider) : INotificationCommandUseCase
{
    public async Task<Result> MarkAsReadAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        if (tenantProvider.CollaboratorId is null)
        {
            return Result.Forbidden("Colaborador não identificado.");
        }

        var notification = await notificationRepository.GetByIdAsync(notificationId, cancellationToken);
        if (notification is null)
        {
            return Result.NotFound("Notificação não encontrada.");
        }

        if (notification.RecipientCollaboratorId != tenantProvider.CollaboratorId.Value)
        {
            return Result.Forbidden("Você não tem permissão para marcar esta notificação como lida.");
        }

        if (notification.IsRead)
        {
            return Result.Success();
        }

        notification.MarkAsRead(DateTime.UtcNow);
        await notificationRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> MarkAllAsReadAsync(
        CancellationToken cancellationToken = default)
    {
        if (tenantProvider.CollaboratorId is null)
        {
            return Result.Forbidden("Colaborador não identificado.");
        }

        await notificationRepository.MarkAllAsReadAsync(
            tenantProvider.CollaboratorId.Value,
            cancellationToken);

        return Result.Success();
    }
}
