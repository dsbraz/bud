using Bud.Application.Common;
using Bud.Application.Ports;

namespace Bud.Application.Features.Notifications.UseCases;

public sealed class ListNotifications(
    INotificationRepository notificationRepository,
    ITenantProvider tenantProvider)
{
    public async Task<Result<PagedResult<NotificationResponse>>> ExecuteAsync(
        bool? isRead,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (tenantProvider.CollaboratorId is null)
        {
            return Result<PagedResult<NotificationResponse>>.Forbidden(UserErrorMessages.CollaboratorNotIdentified);
        }

        var pagedSummaries = await notificationRepository.GetByRecipientAsync(
            tenantProvider.CollaboratorId.Value,
            isRead,
            page,
            pageSize,
            cancellationToken);

        return Result<PagedResult<NotificationResponse>>.Success(pagedSummaries.MapPaged(n => n.ToResponse()));
    }
}
