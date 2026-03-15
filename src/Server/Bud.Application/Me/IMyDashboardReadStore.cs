namespace Bud.Application.Me;

public interface IMyDashboardReadStore
{
    Task<DashboardSnapshot?> GetMyDashboardAsync(
        Guid collaboratorId,
        Guid? teamId,
        CancellationToken ct = default);
}
