using Bud.Server.Application.Common;
using Bud.Server.Infrastructure.Persistence;
using Bud.Server.Domain.Model;
using Microsoft.EntityFrameworkCore;

using Bud.Shared.Contracts;

namespace Bud.Server.Infrastructure.Repositories;

public sealed class NotificationRepository(ApplicationDbContext dbContext) : INotificationRepository
{
    public async Task<Notification?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == id, ct);

    public async Task<PagedResult<Notification>> GetByRecipientAsync(
        Guid recipientId,
        bool? isRead,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);

        var query = dbContext.Notifications
            .AsNoTracking()
            .Where(n => n.RecipientCollaboratorId == recipientId)
            .OrderByDescending(n => n.CreatedAtUtc);

        if (isRead.HasValue)
        {
            query = query.Where(n => n.IsRead == isRead.Value)
                .OrderByDescending(n => n.CreatedAtUtc);
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<Notification>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task MarkAllAsReadAsync(Guid recipientId, CancellationToken ct = default)
        => await dbContext.Notifications
            .Where(n => n.RecipientCollaboratorId == recipientId && !n.IsRead)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAtUtc, DateTime.UtcNow),
                ct);

    public async Task AddRangeAsync(IEnumerable<Notification> notifications, CancellationToken ct = default)
        => await dbContext.Notifications.AddRangeAsync(notifications, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await dbContext.SaveChangesAsync(ct);
}
