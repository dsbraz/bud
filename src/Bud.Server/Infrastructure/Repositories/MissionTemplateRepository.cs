using Bud.Server.Domain.Specifications;
using Bud.Server.Infrastructure.Persistence;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bud.Server.Infrastructure.Repositories;

public sealed class MissionTemplateRepository(ApplicationDbContext dbContext) : IMissionTemplateRepository
{
    public async Task<MissionTemplate?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await dbContext.MissionTemplates.FindAsync([id], ct);

    public async Task<MissionTemplate?> GetByIdWithChildrenAsync(Guid id, CancellationToken ct = default)
        => await dbContext.MissionTemplates
            .Include(t => t.Objectives)
            .Include(t => t.Metrics)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<MissionTemplate?> GetByIdReadOnlyAsync(Guid id, CancellationToken ct = default)
        => await dbContext.MissionTemplates
            .AsNoTracking()
            .Include(t => t.Objectives.OrderBy(o => o.OrderIndex))
            .Include(t => t.Metrics.OrderBy(m => m.OrderIndex))
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<PagedResult<MissionTemplate>> GetAllAsync(string? search, int page, int pageSize, CancellationToken ct = default)
    {
        var query = dbContext.MissionTemplates
            .AsNoTracking()
            .Include(t => t.Objectives.OrderBy(o => o.OrderIndex))
            .Include(t => t.Metrics.OrderBy(m => m.OrderIndex));

        IQueryable<MissionTemplate> filteredQuery = new MissionTemplateSearchSpecification(search, dbContext.Database.IsNpgsql()).Apply(query);

        var total = await filteredQuery.CountAsync(ct);
        var items = await filteredQuery
            .OrderBy(t => t.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<MissionTemplate>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task AddAsync(MissionTemplate entity, CancellationToken ct = default)
        => await dbContext.MissionTemplates.AddAsync(entity, ct);

    public async Task RemoveAsync(MissionTemplate entity, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        dbContext.MissionTemplates.Remove(entity);
    }

    public async Task RemoveObjectivesAndMetrics(IEnumerable<MissionTemplateObjective> objectives, IEnumerable<MissionTemplateMetric> metrics, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        dbContext.MissionTemplateMetrics.RemoveRange(metrics);
        dbContext.MissionTemplateObjectives.RemoveRange(objectives);
    }

    public async Task AddObjectivesAndMetrics(IEnumerable<MissionTemplateObjective> objectives, IEnumerable<MissionTemplateMetric> metrics, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        dbContext.MissionTemplateObjectives.AddRange(objectives);
        dbContext.MissionTemplateMetrics.AddRange(metrics);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await dbContext.SaveChangesAsync(ct);
}
