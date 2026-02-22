using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.MultiTenancy;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.Me;

public sealed class GetMyDashboard(
    IMyDashboardReadStore dashboardReadStore,
    ITenantProvider tenantProvider)
{
    public async Task<Result<MyDashboardResponse>> ExecuteAsync(
        ClaimsPrincipal user,
        Guid? teamId = null,
        CancellationToken cancellationToken = default)
    {
        _ = user;

        if (!tenantProvider.CollaboratorId.HasValue)
        {
            return Result<MyDashboardResponse>.Forbidden("Colaborador não identificado.");
        }

        var snapshot = await dashboardReadStore.GetMyDashboardAsync(
            tenantProvider.CollaboratorId.Value,
            teamId,
            cancellationToken);

        if (snapshot is null)
        {
            return Result<MyDashboardResponse>.NotFound("Colaborador não encontrado.");
        }

        return Result<MyDashboardResponse>.Success(snapshot.ToContract());
    }
}
