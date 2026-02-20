using Bud.Server.Application.Common;
namespace Bud.Server.Application.Notifications;

public interface INotificationCommandUseCase
{
    Task<Result> MarkAsReadAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default);

    Task<Result> MarkAllAsReadAsync(
        CancellationToken cancellationToken = default);
}
