using Bud.Server.Infrastructure.Querying;
using Bud.Server.Infrastructure.Persistence;
using Bud.Shared.Contracts;
using Bud.Server.Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace Bud.Server.Infrastructure.Repositories;

public sealed class MissionMetricRepository(ApplicationDbContext dbContext) : IMissionMetricRepository
{
    public async Task<MissionMetric?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await dbContext.MissionMetrics
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task<MissionMetric?> GetByIdTrackingAsync(Guid id, CancellationToken ct = default)
        => await dbContext.MissionMetrics.FindAsync([id], ct);

    public async Task<PagedResult<MissionMetric>> GetAllAsync(
        Guid? missionId, Guid? objectiveId, string? search, int page, int pageSize, CancellationToken ct = default)
    {
        var query = dbContext.MissionMetrics.AsNoTracking();

        if (missionId.HasValue)
        {
            query = query.Where(m => m.MissionId == missionId.Value);
        }

        if (objectiveId.HasValue)
        {
            query = query.Where(m => m.MissionObjectiveId == objectiveId.Value);
        }

        query = new MissionMetricSearchSpecification(search, dbContext.Database.IsNpgsql()).Apply(query);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(m => m.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<MissionMetric>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<Mission?> GetMissionByIdAsync(Guid missionId, CancellationToken ct = default)
        => await dbContext.Missions
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == missionId, ct);

    public async Task<MissionObjective?> GetObjectiveByIdAsync(Guid objectiveId, CancellationToken ct = default)
        => await dbContext.MissionObjectives
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == objectiveId, ct);

    public async Task AddAsync(MissionMetric entity, CancellationToken ct = default)
        => await dbContext.MissionMetrics.AddAsync(entity, ct);

    public Task RemoveAsync(MissionMetric entity, CancellationToken ct = default)
    {
        dbContext.MissionMetrics.Remove(entity);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await dbContext.SaveChangesAsync(ct);
}
