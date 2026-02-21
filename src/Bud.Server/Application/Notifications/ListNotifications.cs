using Bud.Server.Application.Common;
using Bud.Server.Domain.Repositories;
using Bud.Server.MultiTenancy;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.Notifications;

public sealed class ListNotifications(
    INotificationRepository notificationRepository,
    ITenantProvider tenantProvider)
{
    public async Task<Result<PagedResult<NotificationDto>>> ExecuteAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (tenantProvider.CollaboratorId is null)
        {
            return Result<PagedResult<NotificationDto>>.Forbidden("Colaborador não identificado.");
        }

        var pagedSummaries = await notificationRepository.GetByRecipientAsync(
            tenantProvider.CollaboratorId.Value,
            page,
            pageSize,
            cancellationToken);

        return Result<PagedResult<NotificationDto>>.Success(pagedSummaries.MapPaged(n => n.ToContract()));
    }
}
