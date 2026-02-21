using Bud.Server.Application.Projections;

namespace Bud.Server.Domain.Repositories;

public interface IDashboardReadRepository
{
    Task<MyDashboardSnapshot?> GetMyDashboardAsync(Guid collaboratorId, Guid? teamId, CancellationToken ct = default);
}
