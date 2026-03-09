using Bud.Application.Common;
using Bud.Application.Mapping;
using Bud.Domain.Repositories;
using Bud.Application.Ports;

namespace Bud.Application.UseCases.Notifications;

public sealed class PatchNotifications(
    INotificationRepository notificationRepository,
    ITenantProvider tenantProvider)
{
    public async Task<Result> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (tenantProvider.CollaboratorId is null)
        {
            return Result.Forbidden(UserErrorMessages.CollaboratorNotIdentified);
        }

        await notificationRepository.MarkAllAsReadAsync(
            tenantProvider.CollaboratorId.Value,
            cancellationToken);

        return Result.Success();
    }
}
