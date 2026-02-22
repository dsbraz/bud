using Bud.Server.Application.Common;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Server.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Bud.Shared.Contracts;

namespace Bud.Server.Infrastructure.Repositories;

public sealed class ObjectiveRepository(ApplicationDbContext dbContext) : IObjectiveRepository
{
    public async Task<Objective?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await dbContext.MissionObjectives
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<Objective?> GetByIdTrackedAsync(Guid id, CancellationToken ct = default)
        => await dbContext.MissionObjectives.FindAsync([id], ct);

    public async Task<PagedResult<Objective>> GetAllAsync(
        Guid? missionId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);

        var query = dbContext.MissionObjectives.AsNoTracking();
        if (missionId.HasValue)
        {
            query = query.Where(o => o.MissionId == missionId.Value);
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(o => o.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<Objective>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task AddAsync(Objective entity, CancellationToken ct = default)
        => await dbContext.MissionObjectives.AddAsync(entity, ct);

    public Task RemoveAsync(Objective entity, CancellationToken ct = default)
    {
        dbContext.MissionObjectives.Remove(entity);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await dbContext.SaveChangesAsync(ct);
}
