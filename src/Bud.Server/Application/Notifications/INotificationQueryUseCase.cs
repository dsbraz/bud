using Bud.Shared.Contracts;

namespace Bud.Server.Application.Notifications;

public interface INotificationQueryUseCase
{
    Task<ServiceResult<PagedResult<NotificationDto>>> GetMyNotificationsAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<int>> GetUnreadCountAsync(
        CancellationToken cancellationToken = default);
}
