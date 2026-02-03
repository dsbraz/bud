using Bud.Server.Data;
using Bud.Shared.Contracts;
using Bud.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Bud.Server.Services;

public sealed class MissionMetricService(ApplicationDbContext dbContext) : IMissionMetricService
{
    public async Task<ServiceResult<MissionMetric>> CreateAsync(CreateMissionMetricRequest request, CancellationToken cancellationToken = default)
    {
        var mission = await dbContext.Missions
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == request.MissionId, cancellationToken);

        if (mission is null)
        {
            return ServiceResult<MissionMetric>.NotFound("Mission not found.");
        }

        var metric = new MissionMetric
        {
            Id = Guid.NewGuid(),
            OrganizationId = mission.OrganizationId,
            MissionId = request.MissionId,
            Name = request.Name.Trim(),
            Type = request.Type,
        };

        ApplyMetricTarget(metric, request.Type, request.QuantitativeType, request.MinValue, request.MaxValue, request.Unit, request.TargetText);

        dbContext.MissionMetrics.Add(metric);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<MissionMetric>.Success(metric);
    }

    public async Task<ServiceResult<MissionMetric>> UpdateAsync(Guid id, UpdateMissionMetricRequest request, CancellationToken cancellationToken = default)
    {
        var metric = await dbContext.MissionMetrics.FindAsync([id], cancellationToken);

        if (metric is null)
        {
            return ServiceResult<MissionMetric>.NotFound("Mission metric not found.");
        }

        metric.Name = request.Name.Trim();
        metric.Type = request.Type;
        ApplyMetricTarget(metric, request.Type, request.QuantitativeType, request.MinValue, request.MaxValue, request.Unit, request.TargetText);

        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<MissionMetric>.Success(metric);
    }

    public async Task<ServiceResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var metric = await dbContext.MissionMetrics.FindAsync([id], cancellationToken);

        if (metric is null)
        {
            return ServiceResult.NotFound("Mission metric not found.");
        }

        dbContext.MissionMetrics.Remove(metric);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult<MissionMetric>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var metric = await dbContext.MissionMetrics
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        return metric is null
            ? ServiceResult<MissionMetric>.NotFound("Mission metric not found.")
            : ServiceResult<MissionMetric>.Success(metric);
    }

    public async Task<ServiceResult<PagedResult<MissionMetric>>> GetAllAsync(Guid? missionId, string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 10 : pageSize;

        var query = dbContext.MissionMetrics.AsNoTracking();

        if (missionId.HasValue)
        {
            query = query.Where(m => m.MissionId == missionId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(m => m.Name.Contains(search.Trim()));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(m => m.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var result = new PagedResult<MissionMetric>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };

        return ServiceResult<PagedResult<MissionMetric>>.Success(result);
    }

    private static void ApplyMetricTarget(MissionMetric metric, MetricType type, QuantitativeMetricType? quantitativeType, decimal? minValue, decimal? maxValue, MetricUnit? unit, string? targetText)
    {
        if (type == MetricType.Qualitative)
        {
            metric.TargetText = targetText?.Trim();
            metric.QuantitativeType = null;
            metric.MinValue = null;
            metric.MaxValue = null;
            metric.Unit = null;
            return;
        }

        metric.QuantitativeType = quantitativeType;
        metric.MinValue = minValue;
        metric.MaxValue = maxValue;
        metric.Unit = unit;
        metric.TargetText = null;
    }
}
