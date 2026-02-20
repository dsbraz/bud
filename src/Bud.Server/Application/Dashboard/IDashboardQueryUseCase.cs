using System.Security.Claims;
using Bud.Shared.Contracts;
using Bud.Server.Application.Common;

namespace Bud.Server.Application.Dashboard;

public interface IDashboardQueryUseCase
{
    Task<Result<MyDashboardResponse>> GetMyDashboardAsync(ClaimsPrincipal user, Guid? teamId = null, CancellationToken cancellationToken = default);
}
