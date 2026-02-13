using Bud.Server.Services;
using Bud.Server.MultiTenancy;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.Notifications;

public sealed class NotificationQueryUseCase(
    INotificationService notificationService,
    ITenantProvider tenantProvider) : INotificationQueryUseCase
{
    public Task<ServiceResult<PagedResult<NotificationDto>>> GetMyNotificationsAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (tenantProvider.CollaboratorId is null)
        {
            return Task.FromResult(
                ServiceResult<PagedResult<NotificationDto>>.Forbidden("Colaborador não identificado."));
        }

        return notificationService.GetByRecipientAsync(
            tenantProvider.CollaboratorId.Value,
            page,
            pageSize,
            cancellationToken);
    }

    public Task<ServiceResult<int>> GetUnreadCountAsync(
        CancellationToken cancellationToken = default)
    {
        if (tenantProvider.CollaboratorId is null)
        {
            return Task.FromResult(ServiceResult<int>.Forbidden("Colaborador não identificado."));
        }

        return notificationService.GetUnreadCountAsync(
            tenantProvider.CollaboratorId.Value,
            cancellationToken);
    }
}
