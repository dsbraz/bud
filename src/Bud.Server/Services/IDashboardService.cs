using Bud.Shared.Contracts;

namespace Bud.Server.Services;

public interface IDashboardService
{
    Task<ServiceResult<MyDashboardResponse>> GetMyDashboardAsync(Guid collaboratorId, Guid? teamId = null, CancellationToken cancellationToken = default);
}
