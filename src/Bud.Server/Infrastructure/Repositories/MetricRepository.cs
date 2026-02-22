using Bud.Server.Infrastructure.Querying;
using Bud.Server.Infrastructure.Persistence;
using Bud.Server.Application.Common;
using Bud.Server.Domain.Model;
using Microsoft.EntityFrameworkCore;

using Bud.Shared.Contracts;

namespace Bud.Server.Infrastructure.Repositories;

public sealed class MetricRepository(ApplicationDbContext dbContext) : IMetricRepository
{
    public async Task<Metric?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await dbContext.MissionMetrics
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task<Metric?> GetByIdTrackingAsync(Guid id, CancellationToken ct = default)
        => await dbContext.MissionMetrics.FindAsync([id], ct);

    public async Task<PagedResult<Metric>> GetAllAsync(
        Guid? missionId, Guid? objectiveId, string? search, int page, int pageSize, CancellationToken ct = default)
    {
        var query = dbContext.MissionMetrics.AsNoTracking();

        if (missionId.HasValue)
        {
            query = query.Where(m => m.MissionId == missionId.Value);
        }

        if (objectiveId.HasValue)
        {
            query = query.Where(m => m.ObjectiveId == objectiveId.Value);
        }

        query = new MissionMetricSearchSpecification(search, dbContext.Database.IsNpgsql()).Apply(query);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(m => m.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<Metric>
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

    public async Task<Objective?> GetObjectiveByIdAsync(Guid objectiveId, CancellationToken ct = default)
        => await dbContext.MissionObjectives
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == objectiveId, ct);

    public async Task<MetricCheckin?> GetCheckinByIdAsync(Guid id, CancellationToken ct = default)
        => await dbContext.MetricCheckins
            .AsNoTracking()
            .FirstOrDefaultAsync(mc => mc.Id == id, ct);

    public async Task<PagedResult<MetricCheckin>> GetCheckinsAsync(
        Guid? metricId, Guid? missionId, int page, int pageSize, CancellationToken ct = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);

        var query = dbContext.MetricCheckins.AsNoTracking();

        if (metricId.HasValue)
        {
            query = query.Where(mc => mc.MetricId == metricId.Value);
        }

        if (missionId.HasValue)
        {
            query = query.Where(mc => mc.Metric.MissionId == missionId.Value);
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(mc => mc.CheckinDate)
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

        return new PagedResult<MetricCheckin>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<Metric?> GetMetricWithMissionAsync(Guid metricId, CancellationToken ct = default)
        => await dbContext.MissionMetrics
            .AsNoTracking()
            .Include(m => m.Mission)
            .FirstOrDefaultAsync(m => m.Id == metricId, ct);

    public async Task AddCheckinAsync(MetricCheckin entity, CancellationToken ct = default)
        => await dbContext.MetricCheckins.AddAsync(entity, ct);

    public Task RemoveCheckinAsync(MetricCheckin entity, CancellationToken ct = default)
    {
        dbContext.MetricCheckins.Remove(entity);
        return Task.CompletedTask;
    }

    public async Task AddAsync(Metric entity, CancellationToken ct = default)
        => await dbContext.MissionMetrics.AddAsync(entity, ct);

    public Task RemoveAsync(Metric entity, CancellationToken ct = default)
    {
        dbContext.MissionMetrics.Remove(entity);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await dbContext.SaveChangesAsync(ct);
}
