using Bud.Server.Application.Abstractions;
using Bud.Server.MultiTenancy;

namespace Bud.Server.Application.Notifications;

public sealed class NotificationCommandUseCase(
    INotificationService notificationService,
    ITenantProvider tenantProvider) : INotificationCommandUseCase
{
    public Task<ServiceResult> MarkAsReadAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        if (tenantProvider.CollaboratorId is null)
        {
            return Task.FromResult(ServiceResult.Forbidden("Colaborador não identificado."));
        }

        return notificationService.MarkAsReadAsync(
            notificationId,
            tenantProvider.CollaboratorId.Value,
            cancellationToken);
    }

    public Task<ServiceResult> MarkAllAsReadAsync(
        CancellationToken cancellationToken = default)
    {
        if (tenantProvider.CollaboratorId is null)
        {
            return Task.FromResult(ServiceResult.Forbidden("Colaborador não identificado."));
        }

        return notificationService.MarkAllAsReadAsync(
            tenantProvider.CollaboratorId.Value,
            cancellationToken);
    }
}
