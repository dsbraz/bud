using Bud.Server.Application.Common;
using Bud.Server.Domain.Repositories;
using Bud.Server.MultiTenancy;

namespace Bud.Server.Application.Notifications;

public sealed class PatchNotifications(
    INotificationRepository notificationRepository,
    ITenantProvider tenantProvider)
{
    public async Task<Result> ExecuteAsync(CancellationToken cancellationToken = default)
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
