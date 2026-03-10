using Bud.Application.Common;
using Bud.Application.Ports;

namespace Bud.Application.Notifications;

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
            return Result.Forbidden(UserErrorMessages.CollaboratorNotIdentified);
        }

        var notification = await notificationRepository.GetByIdAsync(notificationId, cancellationToken);
        if (notification is null)
        {
            return Result.NotFound(UserErrorMessages.NotificationNotFound);
        }

        if (notification.RecipientCollaboratorId != tenantProvider.CollaboratorId.Value)
        {
            return Result.Forbidden(UserErrorMessages.NotificationPatchForbidden);
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
