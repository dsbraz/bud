using Bud.Server.Domain.ReadModels;

namespace Bud.Server.Services;

public interface IDashboardService
{
    Task<ServiceResult<MyDashboardSnapshot>> GetMyDashboardAsync(Guid collaboratorId, Guid? teamId = null, CancellationToken cancellationToken = default);
}
