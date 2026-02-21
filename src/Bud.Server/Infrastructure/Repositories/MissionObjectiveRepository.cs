using Bud.Server.Application.Common;
using Bud.Server.Infrastructure.Persistence;
using Bud.Shared.Contracts;
using Bud.Server.Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace Bud.Server.Infrastructure.Repositories;

public sealed class MissionObjectiveRepository(ApplicationDbContext dbContext) : IMissionObjectiveRepository
{
    public async Task<MissionObjective?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await dbContext.MissionObjectives
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<MissionObjective?> GetByIdTrackedAsync(Guid id, CancellationToken ct = default)
        => await dbContext.MissionObjectives.FindAsync([id], ct);

    public async Task<PagedResult<MissionObjective>> GetByMissionAsync(
        Guid missionId, int page, int pageSize, CancellationToken ct = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);

        var query = dbContext.MissionObjectives
            .AsNoTracking()
            .Where(o => o.MissionId == missionId);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(o => o.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<MissionObjective>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<Mission?> GetMissionAsync(Guid missionId, CancellationToken ct = default)
        => await dbContext.Missions
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == missionId, ct);

    public async Task<bool> DimensionBelongsToOrganizationAsync(
        Guid dimensionId, Guid organizationId, CancellationToken ct = default)
        => await dbContext.ObjectiveDimensions
            .AsNoTracking()
            .AnyAsync(d => d.Id == dimensionId && d.OrganizationId == organizationId, ct);

    public async Task AddAsync(MissionObjective entity, CancellationToken ct = default)
        => await dbContext.MissionObjectives.AddAsync(entity, ct);

    public Task RemoveAsync(MissionObjective entity, CancellationToken ct = default)
    {
        dbContext.MissionObjectives.Remove(entity);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await dbContext.SaveChangesAsync(ct);
}
