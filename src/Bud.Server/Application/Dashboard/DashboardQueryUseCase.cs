using System.Security.Claims;
using Bud.Server.MultiTenancy;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.Dashboard;

public sealed class DashboardQueryUseCase(
    IDashboardService dashboardService,
    ITenantProvider tenantProvider) : IDashboardQueryUseCase
{
    public Task<ServiceResult<MyDashboardResponse>> GetMyDashboardAsync(
        ClaimsPrincipal user,
        Guid? teamId = null,
        CancellationToken cancellationToken = default)
    {
        _ = user;

        if (!tenantProvider.CollaboratorId.HasValue)
        {
            return Task.FromResult(ServiceResult<MyDashboardResponse>.Forbidden("Colaborador não identificado."));
        }

        return dashboardService.GetMyDashboardAsync(tenantProvider.CollaboratorId.Value, teamId, cancellationToken);
    }
}
