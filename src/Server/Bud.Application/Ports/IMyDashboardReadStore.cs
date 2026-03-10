
namespace Bud.Application.Ports;

public interface IMyDashboardReadStore
{
    Task<DashboardSnapshot?> GetMyDashboardAsync(
        Guid collaboratorId,
        Guid? teamId,
        CancellationToken ct = default);
}
