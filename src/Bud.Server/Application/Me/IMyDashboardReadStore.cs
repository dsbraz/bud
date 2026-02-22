using Bud.Server.Domain.ReadModels;

namespace Bud.Server.Application.Me;

public interface IMyDashboardReadStore
{
    Task<MyDashboardSnapshot?> GetMyDashboardAsync(
        Guid collaboratorId,
        Guid? teamId,
        CancellationToken ct = default);
}
