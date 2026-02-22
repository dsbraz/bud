using Bud.Server.Domain.ReadModels;

namespace Bud.Server.Application.Ports;

public interface IMyDashboardReadStore
{
    Task<MyDashboardSnapshot?> GetMyDashboardAsync(
        Guid collaboratorId,
        Guid? teamId,
        CancellationToken ct = default);
}
