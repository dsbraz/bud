using Bud.Server.Domain.ReadModels;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Infrastructure.Repositories;

public interface INotificationRepository
{
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<NotificationSummary>> GetByRecipientAsync(Guid recipientId, int page, int pageSize, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(Guid recipientId, CancellationToken ct = default);
    Task MarkAllAsReadAsync(Guid recipientId, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<Notification> notifications, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
