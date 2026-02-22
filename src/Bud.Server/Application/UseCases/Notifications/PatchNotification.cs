using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Domain.Abstractions;
using Bud.Server.Domain.Repositories;
using Bud.Server.MultiTenancy;

namespace Bud.Server.Application.UseCases.Notifications;

public sealed class PatchNotification(
    INotificationRepository notificationRepository,
    ITenantProvider tenantProvider,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result> ExecuteAsync(
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
        await unitOfWork.CommitAsync(notificationRepository.SaveChangesAsync, cancellationToken);

        return Result.Success();
    }
}
