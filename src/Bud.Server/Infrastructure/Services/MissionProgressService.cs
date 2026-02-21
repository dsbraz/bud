using Bud.Server.Infrastructure.Persistence;
using Bud.Server.Application.Projections;
using Bud.Server.Domain.Model;
using Microsoft.EntityFrameworkCore;
using Bud.Server.Application.Common;

namespace Bud.Server.Infrastructure.Services;

public sealed class MissionProgressService(ApplicationDbContext dbContext) : IMissionProgressService
{
    public async Task<Result<List<MissionProgressSnapshot>>> GetProgressAsync(
        List<Guid> missionIds,
        CancellationToken cancellationToken = default)
    {
        if (missionIds.Count == 0)
        {
            return Result<List<MissionProgressSnapshot>>.Success([]);
        }

        var missions = await dbContext.Missions
            .AsNoTracking()
            .Include(m => m.Metrics)
            .Where(m => missionIds.Contains(m.Id))
            .ToListAsync(cancellationToken);

        var allMetricIds = missions
            .SelectMany(m => m.Metrics)
            .Select(m => m.Id)
            .ToList();

        // Fetch all check-ins for the metrics (grouped in memory to avoid
        // GroupBy+OrderBy+First which doesn't translate to SQL in PostgreSQL)
        var allCheckins = await dbContext.MetricCheckins
            .AsNoTracking()
            .Where(c => allMetricIds.Contains(c.MissionMetricId))
            .ToListAsync(cancellationToken);

        var latestCheckinByMetric = allCheckins
            .GroupBy(c => c.MissionMetricId)
            .ToDictionary(g => g.Key, g => g.OrderBy(c => c.CheckinDate).Last());

        // Build first check-in map for Reduce-type metrics (needed for baseline)
        var reduceMetricIds = missions
            .SelectMany(m => m.Metrics)
            .Where(m => m.Type == MetricType.Quantitative && m.QuantitativeType == QuantitativeMetricType.Reduce)
            .Select(m => m.Id)
            .ToHashSet();

        var firstCheckins = allCheckins
            .Where(c => reduceMetricIds.Contains(c.MissionMetricId))
            .GroupBy(c => c.MissionMetricId)
            .ToDictionary(g => g.Key, g => g.OrderBy(c => c.CheckinDate).First());

        var now = DateTime.UtcNow;
        var oneWeekAgo = now.AddDays(-7);
        var results = new List<MissionProgressSnapshot>();

        foreach (var mission in missions)
        {
            var totalMetrics = mission.Metrics.Count;
            var metricsWithCheckins = 0;
            var outdatedMetrics = 0;
            var progressSum = 0m;
            var confidenceSum = 0m;

            foreach (var metric in mission.Metrics)
            {
                if (!latestCheckinByMetric.TryGetValue(metric.Id, out var latestCheckin))
                {
                    // No check-in at all - counts as outdated
                    outdatedMetrics++;
                    continue;
                }

                metricsWithCheckins++;
                confidenceSum += latestCheckin.ConfidenceLevel;

                // Check if last check-in is older than 7 days
                if (latestCheckin.CheckinDate < oneWeekAgo)
                {
                    outdatedMetrics++;
                }

                var metricProgress = CalculateMetricProgress(metric, latestCheckin, firstCheckins);
                progressSum += metricProgress;
            }

            var overallProgress = totalMetrics > 0 && metricsWithCheckins > 0
                ? progressSum / totalMetrics
                : 0m;

            var averageConfidence = metricsWithCheckins > 0
                ? confidenceSum / metricsWithCheckins
                : 0m;

            var expectedProgress = CalculateExpectedProgress(mission.StartDate, mission.EndDate, now);

            // Build objective-level progress breakdown
            var objectiveProgress = BuildObjectiveProgress(
                mission.Metrics, latestCheckinByMetric, firstCheckins, oneWeekAgo);

            results.Add(new MissionProgressSnapshot
            {
                MissionId = mission.Id,
                OverallProgress = Math.Round(overallProgress, 1),
                ExpectedProgress = Math.Round(expectedProgress, 1),
                AverageConfidence = Math.Round(averageConfidence, 1),
                TotalMetrics = totalMetrics,
                MetricsWithCheckins = metricsWithCheckins,
                OutdatedMetrics = outdatedMetrics,
                ObjectiveProgress = objectiveProgress
            });
        }

        return Result<List<MissionProgressSnapshot>>.Success(results);
    }

    public async Task<Result<List<MetricProgressSnapshot>>> GetMetricProgressAsync(
        List<Guid> metricIds,
        CancellationToken cancellationToken = default)
    {
        if (metricIds.Count == 0)
        {
            return Result<List<MetricProgressSnapshot>>.Success([]);
        }

        var metrics = await dbContext.MissionMetrics
            .AsNoTracking()
            .Where(m => metricIds.Contains(m.Id))
            .ToListAsync(cancellationToken);

        var allCheckins = await dbContext.MetricCheckins
            .AsNoTracking()
            .Where(c => metricIds.Contains(c.MissionMetricId))
            .ToListAsync(cancellationToken);

        var latestCheckinByMetric = allCheckins
            .GroupBy(c => c.MissionMetricId)
            .ToDictionary(g => g.Key, g => g.OrderBy(c => c.CheckinDate).Last());

        var reduceMetricIds = metrics
            .Where(m => m.Type == MetricType.Quantitative && m.QuantitativeType == QuantitativeMetricType.Reduce)
            .Select(m => m.Id)
            .ToHashSet();

        var firstCheckins = allCheckins
            .Where(c => reduceMetricIds.Contains(c.MissionMetricId))
            .GroupBy(c => c.MissionMetricId)
            .ToDictionary(g => g.Key, g => g.OrderBy(c => c.CheckinDate).First());

        var collaboratorIds = latestCheckinByMetric.Values
            .Select(c => c.CollaboratorId)
            .Distinct()
            .ToList();

        var collaboratorNames = await dbContext.Collaborators
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(c => collaboratorIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.FullName, cancellationToken);

        var now = DateTime.UtcNow;
        var oneWeekAgo = now.AddDays(-7);
        var results = new List<MetricProgressSnapshot>();

        foreach (var metric in metrics)
        {
            if (!latestCheckinByMetric.TryGetValue(metric.Id, out var latestCheckin))
            {
                results.Add(new MetricProgressSnapshot
                {
                    MetricId = metric.Id,
                    Progress = 0m,
                    Confidence = 0,
                    HasCheckins = false,
                    IsOutdated = true
                });
                continue;
            }

            var progress = CalculateMetricProgress(metric, latestCheckin, firstCheckins);
            var isOutdated = latestCheckin.CheckinDate < oneWeekAgo;

            results.Add(new MetricProgressSnapshot
            {
                MetricId = metric.Id,
                Progress = Math.Round(progress, 1),
                Confidence = latestCheckin.ConfidenceLevel,
                HasCheckins = true,
                IsOutdated = isOutdated,
                LastCheckinCollaboratorName = collaboratorNames.GetValueOrDefault(latestCheckin.CollaboratorId)
            });
        }

        return Result<List<MetricProgressSnapshot>>.Success(results);
    }

    public static decimal CalculateMetricProgress(
        MissionMetric metric,
        MetricCheckin latestCheckin,
        Dictionary<Guid, MetricCheckin> firstCheckins)
    {
        if (metric.Type == MetricType.Qualitative)
        {
            return Clamp(latestCheckin.ConfidenceLevel / 5m * 100m);
        }

        if (latestCheckin.Value is null)
        {
            return 0m;
        }

        var value = latestCheckin.Value.Value;

        return metric.QuantitativeType switch
        {
            QuantitativeMetricType.Achieve => CalculateAchieveProgress(value, metric.MaxValue),
            QuantitativeMetricType.Reduce => CalculateReduceProgress(value, metric.MaxValue, metric.Id, firstCheckins),
            QuantitativeMetricType.KeepAbove => CalculateKeepAboveProgress(value, metric.MinValue ?? 0m),
            QuantitativeMetricType.KeepBelow => CalculateKeepBelowProgress(value, metric.MaxValue ?? decimal.MaxValue),
            QuantitativeMetricType.KeepBetween => CalculateKeepBetweenProgress(value, metric.MinValue ?? 0m, metric.MaxValue ?? decimal.MaxValue),
            _ => 0m
        };
    }

    private static decimal CalculateAchieveProgress(decimal currentValue, decimal? maxValue)
    {
        if (maxValue is null or 0m)
        {
            return 0m;
        }

        return Clamp(currentValue / maxValue.Value * 100m);
    }

    private static decimal CalculateReduceProgress(
        decimal currentValue,
        decimal? maxValue,
        Guid metricId,
        Dictionary<Guid, MetricCheckin> firstCheckins)
    {
        var target = maxValue ?? 0m;

        if (currentValue <= target)
        {
            return 100m;
        }

        if (!firstCheckins.TryGetValue(metricId, out var firstCheckin) || firstCheckin.Value is null)
        {
            return 0m;
        }

        var baseline = firstCheckin.Value.Value;
        var range = baseline - target;

        if (range <= 0m)
        {
            return 0m;
        }

        return Clamp((baseline - currentValue) / range * 100m);
    }

    private static decimal CalculateKeepAboveProgress(decimal currentValue, decimal minValue)
    {
        if (currentValue >= minValue)
        {
            return 100m;
        }

        if (minValue <= 0m)
        {
            return 0m;
        }

        return Clamp(currentValue / minValue * 100m);
    }

    private static decimal CalculateKeepBelowProgress(decimal currentValue, decimal maxValue)
    {
        if (currentValue <= maxValue)
        {
            return 100m;
        }

        if (currentValue <= 0m)
        {
            return 100m;
        }

        return Clamp(maxValue / currentValue * 100m);
    }

    private static decimal CalculateKeepBetweenProgress(decimal currentValue, decimal minValue, decimal maxValue)
    {
        if (currentValue >= minValue && currentValue <= maxValue)
        {
            return 100m;
        }

        if (currentValue < minValue)
        {
            if (minValue <= 0m)
            {
                return 0m;
            }

            return Clamp(currentValue / minValue * 100m);
        }

        // currentValue > maxValue
        if (currentValue <= 0m)
        {
            return 100m;
        }

        return Clamp(maxValue / currentValue * 100m);
    }

    public static decimal CalculateExpectedProgress(DateTime startDate, DateTime endDate, DateTime now)
    {
        var totalDays = (endDate - startDate).TotalDays;
        if (totalDays <= 0)
        {
            return 100m;
        }

        var elapsedDays = (now - startDate).TotalDays;
        return Clamp((decimal)(elapsedDays / totalDays) * 100m);
    }

    public async Task<Result<List<ObjectiveProgressSnapshot>>> GetObjectiveProgressAsync(
        List<Guid> objectiveIds,
        CancellationToken cancellationToken = default)
    {
        if (objectiveIds.Count == 0)
        {
            return Result<List<ObjectiveProgressSnapshot>>.Success([]);
        }

        var metrics = await dbContext.MissionMetrics
            .AsNoTracking()
            .Where(m => m.MissionObjectiveId.HasValue && objectiveIds.Contains(m.MissionObjectiveId.Value))
            .ToListAsync(cancellationToken);

        var metricIds = metrics.Select(m => m.Id).ToList();

        var allCheckins = await dbContext.MetricCheckins
            .AsNoTracking()
            .Where(c => metricIds.Contains(c.MissionMetricId))
            .ToListAsync(cancellationToken);

        var latestCheckinByMetric = allCheckins
            .GroupBy(c => c.MissionMetricId)
            .ToDictionary(g => g.Key, g => g.OrderBy(c => c.CheckinDate).Last());

        var reduceMetricIds = metrics
            .Where(m => m.Type == MetricType.Quantitative && m.QuantitativeType == QuantitativeMetricType.Reduce)
            .Select(m => m.Id)
            .ToHashSet();

        var firstCheckins = allCheckins
            .Where(c => reduceMetricIds.Contains(c.MissionMetricId))
            .GroupBy(c => c.MissionMetricId)
            .ToDictionary(g => g.Key, g => g.OrderBy(c => c.CheckinDate).First());

        var oneWeekAgo = DateTime.UtcNow.AddDays(-7);
        var results = new List<ObjectiveProgressSnapshot>();

        foreach (var objectiveId in objectiveIds)
        {
            var objectiveMetrics = metrics.Where(m => m.MissionObjectiveId == objectiveId).ToList();
            results.Add(CalculateObjectiveProgress(
                objectiveId, objectiveMetrics, latestCheckinByMetric, firstCheckins, oneWeekAgo));
        }

        return Result<List<ObjectiveProgressSnapshot>>.Success(results);
    }

    private static List<ObjectiveProgressSnapshot> BuildObjectiveProgress(
        ICollection<MissionMetric> allMetrics,
        Dictionary<Guid, MetricCheckin> latestCheckinByMetric,
        Dictionary<Guid, MetricCheckin> firstCheckins,
        DateTime oneWeekAgo)
    {
        var objectiveIds = allMetrics
            .Where(m => m.MissionObjectiveId.HasValue)
            .Select(m => m.MissionObjectiveId!.Value)
            .Distinct()
            .ToList();

        if (objectiveIds.Count == 0)
        {
            return [];
        }

        return objectiveIds.Select(objectiveId =>
        {
            var objectiveMetrics = allMetrics
                .Where(m => m.MissionObjectiveId == objectiveId)
                .ToList();
            return CalculateObjectiveProgress(
                objectiveId, objectiveMetrics, latestCheckinByMetric, firstCheckins, oneWeekAgo);
        }).ToList();
    }

    private static ObjectiveProgressSnapshot CalculateObjectiveProgress(
        Guid objectiveId,
        List<MissionMetric> metrics,
        Dictionary<Guid, MetricCheckin> latestCheckinByMetric,
        Dictionary<Guid, MetricCheckin> firstCheckins,
        DateTime oneWeekAgo)
    {
        var totalMetrics = metrics.Count;
        var metricsWithCheckins = 0;
        var outdatedMetrics = 0;
        var progressSum = 0m;
        var confidenceSum = 0m;

        foreach (var metric in metrics)
        {
            if (!latestCheckinByMetric.TryGetValue(metric.Id, out var latestCheckin))
            {
                outdatedMetrics++;
                continue;
            }

            metricsWithCheckins++;
            confidenceSum += latestCheckin.ConfidenceLevel;

            if (latestCheckin.CheckinDate < oneWeekAgo)
            {
                outdatedMetrics++;
            }

            progressSum += CalculateMetricProgress(metric, latestCheckin, firstCheckins);
        }

        var overallProgress = totalMetrics > 0 && metricsWithCheckins > 0
            ? progressSum / totalMetrics
            : 0m;

        var averageConfidence = metricsWithCheckins > 0
            ? confidenceSum / metricsWithCheckins
            : 0m;

        return new ObjectiveProgressSnapshot
        {
            ObjectiveId = objectiveId,
            OverallProgress = Math.Round(overallProgress, 1),
            AverageConfidence = Math.Round(averageConfidence, 1),
            TotalMetrics = totalMetrics,
            MetricsWithCheckins = metricsWithCheckins,
            OutdatedMetrics = outdatedMetrics
        };
    }

    private static decimal Clamp(decimal value)
    {
        return Math.Max(0m, Math.Min(100m, value));
    }
}
