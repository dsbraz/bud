namespace Bud.Server.Application.Notifications;

public interface INotificationCommandUseCase
{
    Task<ServiceResult> MarkAsReadAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default);

    Task<ServiceResult> MarkAllAsReadAsync(
        CancellationToken cancellationToken = default);
}
