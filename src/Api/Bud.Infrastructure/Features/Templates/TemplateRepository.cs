using Bud.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Bud.Infrastructure.Features.Templates;

public sealed class TemplateRepository(ApplicationDbContext dbContext) : ITemplateRepository
{
    public async Task<Template?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await dbContext.Templates.FindAsync([id], ct);

    public async Task<Template?> GetByIdWithChildrenAsync(Guid id, CancellationToken ct = default)
        => await dbContext.Templates
            .Include(t => t.Goals)
            .Include(t => t.Indicators)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<Template?> GetByIdReadOnlyAsync(Guid id, CancellationToken ct = default)
        => await dbContext.Templates
            .AsNoTracking()
            .Include(t => t.Goals.OrderBy(g => g.OrderIndex))
            .Include(t => t.Indicators.OrderBy(i => i.OrderIndex))
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<PagedResult<Template>> GetAllAsync(string? search, int page, int pageSize, CancellationToken ct = default)
    {
        var query = dbContext.Templates
            .AsNoTracking()
            .Include(t => t.Goals.OrderBy(g => g.OrderIndex))
            .Include(t => t.Indicators.OrderBy(i => i.OrderIndex));

        IQueryable<Template> filteredQuery = new TemplateSearchSpecification(search, dbContext.Database.IsNpgsql()).Apply(query);

        var total = await filteredQuery.CountAsync(ct);
        var items = await filteredQuery
            .OrderBy(t => t.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<Template>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task AddAsync(Template entity, CancellationToken ct = default)
        => await dbContext.Templates.AddAsync(entity, ct);

    public Task RemoveAsync(Template entity, CancellationToken ct = default)
    {
        dbContext.Templates.Remove(entity);
        return Task.CompletedTask;
    }

    public Task RemoveGoalsAndIndicatorsAsync(IEnumerable<TemplateGoal> goals, IEnumerable<TemplateIndicator> indicators, CancellationToken ct = default)
    {
        dbContext.TemplateIndicators.RemoveRange(indicators);
        dbContext.TemplateGoals.RemoveRange(goals);
        return Task.CompletedTask;
    }

    public Task AddGoalsAndIndicatorsAsync(IEnumerable<TemplateGoal> goals, IEnumerable<TemplateIndicator> indicators, CancellationToken ct = default)
    {
        dbContext.TemplateGoals.AddRange(goals);
        dbContext.TemplateIndicators.AddRange(indicators);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await dbContext.SaveChangesAsync(ct);
}
