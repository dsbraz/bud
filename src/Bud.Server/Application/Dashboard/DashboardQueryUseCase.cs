using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.MultiTenancy;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.Dashboard;

public sealed class DashboardQueryUseCase(
    IDashboardService dashboardService,
    ITenantProvider tenantProvider) : IDashboardQueryUseCase
{
    public async Task<ServiceResult<MyDashboardResponse>> GetMyDashboardAsync(
        ClaimsPrincipal user,
        Guid? teamId = null,
        CancellationToken cancellationToken = default)
    {
        _ = user;

        if (!tenantProvider.CollaboratorId.HasValue)
        {
            return ServiceResult<MyDashboardResponse>.Forbidden("Colaborador não identificado.");
        }

        var result = await dashboardService.GetMyDashboardAsync(tenantProvider.CollaboratorId.Value, teamId, cancellationToken);
        if (!result.IsSuccess)
        {
            return ServiceResult<MyDashboardResponse>.Failure(result.Error ?? "Falha ao carregar dashboard.", result.ErrorType);
        }

        return ServiceResult<MyDashboardResponse>.Success(result.Value!.ToContract());
    }
}
