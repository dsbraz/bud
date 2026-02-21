using Bud.Server.Application.Common;
using Bud.Server.Domain.Repositories;
using Bud.Server.MultiTenancy;

namespace Bud.Server.Application.Notifications;

public sealed class GetUnreadNotificationCount(
    INotificationRepository notificationRepository,
    ITenantProvider tenantProvider)
{
    public async Task<Result<int>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (tenantProvider.CollaboratorId is null)
        {
            return Result<int>.Forbidden("Colaborador não identificado.");
        }

        var count = await notificationRepository.GetUnreadCountAsync(
            tenantProvider.CollaboratorId.Value,
            cancellationToken);

        return Result<int>.Success(count);
    }
}
