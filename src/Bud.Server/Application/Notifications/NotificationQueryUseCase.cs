using Bud.Server.Application.Common;
using Bud.Server.Services;
using Bud.Server.MultiTenancy;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.Notifications;

public sealed class NotificationQueryUseCase(
    INotificationService notificationService,
    ITenantProvider tenantProvider) : INotificationQueryUseCase
{
    public async Task<ServiceResult<PagedResult<NotificationDto>>> GetMyNotificationsAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (tenantProvider.CollaboratorId is null)
        {
            return ServiceResult<PagedResult<NotificationDto>>.Forbidden("Colaborador não identificado.");
        }

        var result = await notificationService.GetByRecipientAsync(
            tenantProvider.CollaboratorId.Value,
            page,
            pageSize,
            cancellationToken);
        if (!result.IsSuccess)
        {
            return ServiceResult<PagedResult<NotificationDto>>.Failure(result.Error ?? "Falha ao carregar notificações.", result.ErrorType);
        }

        return ServiceResult<PagedResult<NotificationDto>>.Success(
            result.Value!.MapPaged(n => n.ToContract()));
    }

    public Task<ServiceResult<int>> GetUnreadCountAsync(
        CancellationToken cancellationToken = default)
    {
        if (tenantProvider.CollaboratorId is null)
        {
            return Task.FromResult(ServiceResult<int>.Forbidden("Colaborador não identificado."));
        }

        return notificationService.GetUnreadCountAsync(
            tenantProvider.CollaboratorId.Value,
            cancellationToken);
    }
}
