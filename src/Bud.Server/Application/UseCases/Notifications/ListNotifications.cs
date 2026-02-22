using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Domain.Repositories;
using Bud.Server.MultiTenancy;

namespace Bud.Server.Application.UseCases.Notifications;

public sealed class ListNotifications(
    INotificationRepository notificationRepository,
    ITenantProvider tenantProvider)
{
    public async Task<Result<Bud.Shared.Contracts.Common.PagedResult<NotificationResponse>>> ExecuteAsync(
        bool? isRead,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (tenantProvider.CollaboratorId is null)
        {
            return Result<Bud.Shared.Contracts.Common.PagedResult<NotificationResponse>>.Forbidden("Colaborador não identificado.");
        }

        var pagedSummaries = await notificationRepository.GetByRecipientAsync(
            tenantProvider.CollaboratorId.Value,
            isRead,
            page,
            pageSize,
            cancellationToken);

        return Result<Bud.Shared.Contracts.Common.PagedResult<NotificationResponse>>.Success(pagedSummaries.MapPaged(n => n.ToResponse()));
    }
}
