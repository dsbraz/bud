using Bud.Infrastructure.Persistence;
using Bud.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace Bud.Infrastructure.Indicators;

public sealed class IndicatorRepository(ApplicationDbContext dbContext) : IIndicatorRepository
{
    public async Task<Indicator?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await dbContext.Indicators
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id, ct);

    public async Task<Indicator?> GetByIdForUpdateAsync(Guid id, CancellationToken ct = default)
        => await dbContext.Indicators.FindAsync([id], ct);

    public async Task<PagedResult<Indicator>> GetAllAsync(
        Guid? goalId, string? search, int page, int pageSize, CancellationToken ct = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);

        var query = dbContext.Indicators.AsNoTracking();

        if (goalId.HasValue)
        {
            query = query.Where(i => i.GoalId == goalId.Value);
        }

        query = new IndicatorSearchSpecification(search, dbContext.Database.IsNpgsql()).Apply(query);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(i => i.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<Indicator>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<Goal?> GetGoalByIdAsync(Guid goalId, CancellationToken ct = default)
        => await dbContext.Goals
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == goalId, ct);

    public async Task<Checkin?> GetCheckinByIdAsync(Guid id, CancellationToken ct = default)
        => await dbContext.Checkins
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<PagedResult<Checkin>> GetCheckinsAsync(
        Guid? indicatorId, Guid? goalId, int page, int pageSize, CancellationToken ct = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);

        var query = dbContext.Checkins.AsNoTracking();

        if (indicatorId.HasValue)
        {
            query = query.Where(c => c.IndicatorId == indicatorId.Value);
        }

        if (goalId.HasValue)
        {
            query = query.Where(c => c.Indicator.GoalId == goalId.Value);
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(c => c.CheckinDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        if (items.Count > 0)
        {
            var collaboratorIds = items.Select(c => c.CollaboratorId).Distinct().ToList();
            var collaborators = await dbContext.Collaborators
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(c => collaboratorIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, ct);

            foreach (var item in items)
            {
                if (collaborators.TryGetValue(item.CollaboratorId, out var collaborator))
                {
                    item.Collaborator = collaborator;
                }
            }
        }

        return new PagedResult<Checkin>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<Indicator?> GetIndicatorWithGoalAsync(Guid indicatorId, CancellationToken ct = default)
        => await dbContext.Indicators
            .AsNoTracking()
            .Include(i => i.Goal)
            .FirstOrDefaultAsync(i => i.Id == indicatorId, ct);

    public async Task AddCheckinAsync(Checkin entity, CancellationToken ct = default)
        => await dbContext.Checkins.AddAsync(entity, ct);

    public Task RemoveCheckinAsync(Checkin entity, CancellationToken ct = default)
    {
        dbContext.Checkins.Remove(entity);
        return Task.CompletedTask;
    }

    public async Task AddAsync(Indicator entity, CancellationToken ct = default)
        => await dbContext.Indicators.AddAsync(entity, ct);

    public Task RemoveAsync(Indicator entity, CancellationToken ct = default)
    {
        dbContext.Indicators.Remove(entity);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await dbContext.SaveChangesAsync(ct);
}
