using System.Security.Claims;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.Dashboard;

public interface IDashboardQueryUseCase
{
    Task<ServiceResult<MyDashboardResponse>> GetMyDashboardAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default);
}
