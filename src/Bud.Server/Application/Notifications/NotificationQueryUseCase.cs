using Bud.Server.Infrastructure.Repositories;
using Bud.Server.Application.Common;
using Bud.Server.MultiTenancy;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.Notifications;

public sealed class NotificationQueryUseCase(
    INotificationRepository notificationRepository,
    ITenantProvider tenantProvider) : INotificationQueryUseCase
{
    public async Task<Result<PagedResult<NotificationDto>>> GetMyNotificationsAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (tenantProvider.CollaboratorId is null)
        {
            return Result<PagedResult<NotificationDto>>.Forbidden("Colaborador não identificado.");
        }

        var pagedSummaries = await notificationRepository.GetByRecipientAsync(
            tenantProvider.CollaboratorId.Value,
            page,
            pageSize,
            cancellationToken);

        return Result<PagedResult<NotificationDto>>.Success(
            pagedSummaries.MapPaged(n => n.ToContract()));
    }

    public async Task<Result<int>> GetUnreadCountAsync(
        CancellationToken cancellationToken = default)
    {
        if (tenantProvider.CollaboratorId is null)
        {
            return Result<int>.Forbidden("Colaborador não identificado.");
        }

        var count = await notificationRepository.GetUnreadCountAsync(
            tenantProvider.CollaboratorId.Value,
            cancellationToken);

        return Result<int>.Success(count);
    }
}
