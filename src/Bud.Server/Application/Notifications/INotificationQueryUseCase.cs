using Bud.Shared.Contracts;
using Bud.Server.Application.Common;

namespace Bud.Server.Application.Notifications;

public interface INotificationQueryUseCase
{
    Task<Result<PagedResult<NotificationDto>>> GetMyNotificationsAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Result<int>> GetUnreadCountAsync(
        CancellationToken cancellationToken = default);
}
