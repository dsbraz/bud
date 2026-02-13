using Bud.Server.Data;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bud.Server.Services;

public sealed class NotificationService(ApplicationDbContext dbContext) : INotificationService
{
    public async Task<ServiceResult> CreateForMultipleRecipientsAsync(
        IEnumerable<Guid> recipientIds,
        Guid organizationId,
        string title,
        string message,
        NotificationType type,
        Guid? relatedEntityId,
        string? relatedEntityType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            var notifications = recipientIds.Select(recipientId => Notification.Create(
                Guid.NewGuid(),
                recipientId,
                organizationId,
                title,
                message,
                type,
                now,
                relatedEntityId,
                relatedEntityType)).ToList();

            if (notifications.Count == 0)
            {
                return ServiceResult.Success();
            }

            dbContext.Notifications.AddRange(notifications);
            await dbContext.SaveChangesAsync(cancellationToken);

            return ServiceResult.Success();
        }
        catch (DomainInvariantException ex)
        {
            return ServiceResult.Failure(ex.Message, ServiceErrorType.Validation);
        }
    }

    public async Task<ServiceResult<PagedResult<NotificationDto>>> GetByRecipientAsync(
        Guid recipientId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);

        var query = dbContext.Notifications
            .AsNoTracking()
            .Where(n => n.RecipientCollaboratorId == recipientId)
            .OrderByDescending(n => n.CreatedAtUtc);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type.ToString(),
                IsRead = n.IsRead,
                CreatedAtUtc = n.CreatedAtUtc,
                ReadAtUtc = n.ReadAtUtc,
                RelatedEntityId = n.RelatedEntityId,
                RelatedEntityType = n.RelatedEntityType
            })
            .ToListAsync(cancellationToken);

        return ServiceResult<PagedResult<NotificationDto>>.Success(new PagedResult<NotificationDto>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        });
    }

    public async Task<ServiceResult<int>> GetUnreadCountAsync(
        Guid recipientId,
        CancellationToken cancellationToken = default)
    {
        var count = await dbContext.Notifications
            .AsNoTracking()
            .CountAsync(n => n.RecipientCollaboratorId == recipientId && !n.IsRead, cancellationToken);

        return ServiceResult<int>.Success(count);
    }

    public async Task<ServiceResult> MarkAsReadAsync(
        Guid notificationId,
        Guid recipientId,
        CancellationToken cancellationToken = default)
    {
        var notification = await dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId, cancellationToken);

        if (notification is null)
        {
            return ServiceResult.NotFound("Notificação não encontrada.");
        }

        if (notification.RecipientCollaboratorId != recipientId)
        {
            return ServiceResult.Forbidden("Você não tem permissão para marcar esta notificação como lida.");
        }

        if (notification.IsRead)
        {
            return ServiceResult.Success();
        }

        notification.MarkAsRead(DateTime.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> MarkAllAsReadAsync(
        Guid recipientId,
        CancellationToken cancellationToken = default)
    {
        await dbContext.Notifications
            .Where(n => n.RecipientCollaboratorId == recipientId && !n.IsRead)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAtUtc, DateTime.UtcNow),
                cancellationToken);

        return ServiceResult.Success();
    }
}
