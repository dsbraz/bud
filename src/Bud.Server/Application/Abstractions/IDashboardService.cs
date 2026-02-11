using Bud.Shared.Contracts;

namespace Bud.Server.Application.Abstractions;

public interface IDashboardService
{
    Task<ServiceResult<MyDashboardResponse>> GetMyDashboardAsync(Guid collaboratorId, CancellationToken cancellationToken = default);
}
