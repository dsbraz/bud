using Bud.Server.Domain.ReadModels;

namespace Bud.Server.Infrastructure.Repositories;

public interface IDashboardReadRepository
{
    Task<MyDashboardSnapshot?> GetMyDashboardAsync(Guid collaboratorId, Guid? teamId, CancellationToken ct = default);
}
