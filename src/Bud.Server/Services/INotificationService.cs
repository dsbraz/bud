using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Services;

public interface INotificationService
{
    Task<ServiceResult> CreateForMultipleRecipientsAsync(
        IEnumerable<Guid> recipientIds,
        Guid organizationId,
        string title,
        string message,
        NotificationType type,
        Guid? relatedEntityId,
        string? relatedEntityType,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<PagedResult<NotificationDto>>> GetByRecipientAsync(
        Guid recipientId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<int>> GetUnreadCountAsync(
        Guid recipientId,
        CancellationToken cancellationToken = default);

    Task<ServiceResult> MarkAsReadAsync(
        Guid notificationId,
        Guid recipientId,
        CancellationToken cancellationToken = default);

    Task<ServiceResult> MarkAllAsReadAsync(
        Guid recipientId,
        CancellationToken cancellationToken = default);
}
