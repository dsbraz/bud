using System.Security.Claims;
using Bud.Application.Common;
using Bud.Application.Ports;
using Bud.Shared.Contracts;

namespace Bud.Application.Me;

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
            return Result<MyDashboardResponse>.Forbidden(UserErrorMessages.CollaboratorNotIdentified);
        }

        var snapshot = await dashboardReadStore.GetMyDashboardAsync(
            tenantProvider.CollaboratorId.Value,
            teamId,
            cancellationToken);

        if (snapshot is null)
        {
            return Result<MyDashboardResponse>.NotFound(UserErrorMessages.CollaboratorNotFound);
        }

        return Result<MyDashboardResponse>.Success(snapshot.ToResponse());
    }
}
