namespace Bud.Application.Features.Me;

public interface IMyDashboardReadStore
{
    Task<DashboardSnapshot?> GetMyDashboardAsync(
        Guid collaboratorId,
        Guid? teamId,
        CancellationToken ct = default);
}
