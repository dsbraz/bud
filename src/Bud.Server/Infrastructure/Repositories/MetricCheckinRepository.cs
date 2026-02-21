using Bud.Server.Application.Common;
using Bud.Server.Infrastructure.Persistence;
using Bud.Shared.Contracts;
using Bud.Server.Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace Bud.Server.Infrastructure.Repositories;

public sealed class MetricCheckinRepository(ApplicationDbContext dbContext) : IMetricCheckinRepository
{
    public async Task<MetricCheckin?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await dbContext.MetricCheckins
            .AsNoTracking()
            .FirstOrDefaultAsync(mc => mc.Id == id, ct);

    public async Task<PagedResult<MetricCheckin>> GetAllAsync(
        Guid? missionMetricId, Guid? missionId, int page, int pageSize, CancellationToken ct = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);

        var baseQuery = dbContext.MetricCheckins.AsNoTracking();

        if (missionMetricId.HasValue)
        {
            baseQuery = baseQuery.Where(mc => mc.MissionMetricId == missionMetricId.Value);
        }

        if (missionId.HasValue)
        {
            baseQuery = baseQuery.Where(mc => mc.MissionMetric.MissionId == missionId.Value);
        }

        var total = await baseQuery.CountAsync(ct);
        var items = await baseQuery
            .OrderByDescending(mc => mc.CheckinDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        // Load Collaborators separately ignoring query filters
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

        return new PagedResult<MetricCheckin>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<MissionMetric?> GetMetricWithMissionAsync(Guid metricId, CancellationToken ct = default)
        => await dbContext.MissionMetrics
            .AsNoTracking()
            .Include(m => m.Mission)
            .FirstOrDefaultAsync(m => m.Id == metricId, ct);

    public async Task<MissionMetric?> GetMetricByIdAsync(Guid metricId, CancellationToken ct = default)
        => await dbContext.MissionMetrics
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == metricId, ct);

    public async Task AddAsync(MetricCheckin entity, CancellationToken ct = default)
        => await dbContext.MetricCheckins.AddAsync(entity, ct);

    public Task RemoveAsync(MetricCheckin entity, CancellationToken ct = default)
    {
        dbContext.MetricCheckins.Remove(entity);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await dbContext.SaveChangesAsync(ct);
}
