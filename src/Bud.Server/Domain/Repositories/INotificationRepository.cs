using Bud.Server.Domain.Model;

namespace Bud.Server.Domain.Repositories;

public interface INotificationRepository
{
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Bud.Shared.Contracts.PagedResult<Notification>> GetByRecipientAsync(
        Guid recipientId,
        bool? isRead,
        int page,
        int pageSize,
        CancellationToken ct = default);
    Task MarkAllAsReadAsync(Guid recipientId, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<Notification> notifications, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
