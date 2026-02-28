using Bud.Server.Infrastructure.Persistence;
using Bud.Server.Application.Ports;
using Bud.Server.Application.ReadModels;
using Bud.Server.Domain.Model;
using Microsoft.EntityFrameworkCore;
using Bud.Server.Application.Common;

namespace Bud.Server.Infrastructure.Services;

public sealed class GoalProgressService(ApplicationDbContext dbContext) : IGoalProgressService
{
    public async Task<Result<List<GoalProgressSnapshot>>> GetProgressAsync(
        List<Guid> goalIds,
        CancellationToken ct = default)
    {
        if (goalIds.Count == 0)
        {
            return Result<List<GoalProgressSnapshot>>.Success([]);
        }

        // Collect all descendant goal IDs for each requested goal
        var descendantMap = await CollectDescendantIdsAsync(goalIds, ct);
        var allGoalIds = goalIds
            .Concat(descendantMap.Values.SelectMany(ids => ids))
            .Distinct()
            .ToList();

        var goals = await dbContext.Goals
            .AsNoTracking()
            .Include(g => g.Indicators)
            .Where(g => allGoalIds.Contains(g.Id))
            .ToListAsync(ct);

        var goalsById = goals.ToDictionary(g => g.Id);

        var allIndicatorIds = goals
            .SelectMany(g => g.Indicators)
            .Select(i => i.Id)
            .ToList();

        // Fetch all checkins for the indicators in memory to avoid
        // GroupBy+OrderBy+First which doesn't translate to SQL in PostgreSQL
        var allCheckins = await dbContext.Checkins
            .AsNoTracking()
            .Where(c => allIndicatorIds.Contains(c.IndicatorId))
            .ToListAsync(ct);

        var latestCheckinByIndicator = allCheckins
            .GroupBy(c => c.IndicatorId)
            .ToDictionary(g => g.Key, g => g.OrderBy(c => c.CheckinDate).Last());

        // Build first checkin map for Reduce-type indicators (needed for baseline)
        var reduceIndicatorIds = goals
            .SelectMany(g => g.Indicators)
            .Where(i => i.Type == IndicatorType.Quantitative && i.QuantitativeType == QuantitativeIndicatorType.Reduce)
            .Select(i => i.Id)
            .ToHashSet();

        var firstCheckins = allCheckins
            .Where(c => reduceIndicatorIds.Contains(c.IndicatorId))
            .GroupBy(c => c.IndicatorId)
            .ToDictionary(g => g.Key, g => g.OrderBy(c => c.CheckinDate).First());

        var now = DateTime.UtcNow;
        var oneWeekAgo = now.AddDays(-7);
        var results = new List<GoalProgressSnapshot>();

        foreach (var goalId in goalIds)
        {
            if (!goalsById.TryGetValue(goalId, out var goal))
            {
                continue;
            }

            // Collect indicators from this goal + all descendants
            var subtreeGoalIds = new List<Guid> { goalId };
            if (descendantMap.TryGetValue(goalId, out var descendants))
            {
                subtreeGoalIds.AddRange(descendants);
            }

            var subtreeIndicators = subtreeGoalIds
                .Where(goalsById.ContainsKey)
                .SelectMany(id => goalsById[id].Indicators)
                .ToList();

            var totalIndicators = subtreeIndicators.Count;
            var indicatorsWithCheckins = 0;
            var outdatedIndicators = 0;
            var progressSum = 0m;
            var confidenceSum = 0m;

            foreach (var indicator in subtreeIndicators)
            {
                if (!latestCheckinByIndicator.TryGetValue(indicator.Id, out var latestCheckin))
                {
                    outdatedIndicators++;
                    continue;
                }

                indicatorsWithCheckins++;
                confidenceSum += latestCheckin.ConfidenceLevel;

                if (latestCheckin.CheckinDate < oneWeekAgo)
                {
                    outdatedIndicators++;
                }

                progressSum += CalculateIndicatorProgress(indicator, latestCheckin, firstCheckins);
            }

            var overallProgress = totalIndicators > 0 && indicatorsWithCheckins > 0
                ? progressSum / totalIndicators
                : 0m;

            var averageConfidence = indicatorsWithCheckins > 0
                ? confidenceSum / indicatorsWithCheckins
                : 0m;

            var expectedProgress = CalculateExpectedProgress(goal.StartDate, goal.EndDate, now);

            var directChildren = goals.Count(g => g.ParentId == goalId);
            var directIndicators = goal.Indicators.Count;

            results.Add(new GoalProgressSnapshot
            {
                GoalId = goal.Id,
                OverallProgress = Math.Round(overallProgress, 1),
                ExpectedProgress = Math.Round(expectedProgress, 1),
                AverageConfidence = Math.Round(averageConfidence, 1),
                TotalIndicators = totalIndicators,
                IndicatorsWithCheckins = indicatorsWithCheckins,
                OutdatedIndicators = outdatedIndicators,
                DirectChildren = directChildren,
                DirectIndicators = directIndicators
            });
        }

        return Result<List<GoalProgressSnapshot>>.Success(results);
    }

    public async Task<Result<IndicatorProgressSnapshot?>> GetIndicatorProgressAsync(
        Guid indicatorId,
        CancellationToken ct = default)
    {
        var indicator = await dbContext.Indicators
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == indicatorId, ct);

        if (indicator is null)
        {
            return Result<IndicatorProgressSnapshot?>.NotFound("Indicador não encontrado.");
        }

        var checkins = await dbContext.Checkins
            .AsNoTracking()
            .Where(c => c.IndicatorId == indicatorId)
            .ToListAsync(ct);

        if (checkins.Count == 0)
        {
            return Result<IndicatorProgressSnapshot?>.Success(new IndicatorProgressSnapshot
            {
                IndicatorId = indicatorId,
                Progress = 0m,
                Confidence = 0,
                HasCheckins = false,
                IsOutdated = true
            });
        }

        var latestCheckin = checkins.OrderBy(c => c.CheckinDate).Last();
        var firstCheckins = new Dictionary<Guid, Checkin>();

        if (indicator.Type == IndicatorType.Quantitative &&
            indicator.QuantitativeType == QuantitativeIndicatorType.Reduce)
        {
            firstCheckins[indicatorId] = checkins.OrderBy(c => c.CheckinDate).First();
        }

        var progress = CalculateIndicatorProgress(indicator, latestCheckin, firstCheckins);
        var isOutdated = latestCheckin.CheckinDate < DateTime.UtcNow.AddDays(-7);

        var collaboratorName = await dbContext.Collaborators
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(c => c.Id == latestCheckin.CollaboratorId)
            .Select(c => (string?)c.FullName)
            .FirstOrDefaultAsync(ct);

        return Result<IndicatorProgressSnapshot?>.Success(new IndicatorProgressSnapshot
        {
            IndicatorId = indicatorId,
            Progress = Math.Round(progress, 1),
            Confidence = latestCheckin.ConfidenceLevel,
            HasCheckins = true,
            IsOutdated = isOutdated,
            LastCheckinCollaboratorName = collaboratorName
        });
    }

    public static decimal CalculateIndicatorProgress(
        Indicator indicator,
        Checkin latestCheckin,
        Dictionary<Guid, Checkin> firstCheckins)
    {
        if (indicator.Type == IndicatorType.Qualitative)
        {
            return Clamp(latestCheckin.ConfidenceLevel / 5m * 100m);
        }

        if (latestCheckin.Value is null)
        {
            return 0m;
        }

        var value = latestCheckin.Value.Value;

        return indicator.QuantitativeType switch
        {
            QuantitativeIndicatorType.Achieve => CalculateAchieveProgress(value, indicator.MaxValue),
            QuantitativeIndicatorType.Reduce => CalculateReduceProgress(value, indicator.MaxValue, indicator.Id, firstCheckins),
            QuantitativeIndicatorType.KeepAbove => CalculateKeepAboveProgress(value, indicator.MinValue ?? 0m),
            QuantitativeIndicatorType.KeepBelow => CalculateKeepBelowProgress(value, indicator.MaxValue ?? decimal.MaxValue),
            QuantitativeIndicatorType.KeepBetween => CalculateKeepBetweenProgress(value, indicator.MinValue ?? 0m, indicator.MaxValue ?? decimal.MaxValue),
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
        Guid indicatorId,
        Dictionary<Guid, Checkin> firstCheckins)
    {
        var target = maxValue ?? 0m;

        if (currentValue <= target)
        {
            return 100m;
        }

        if (!firstCheckins.TryGetValue(indicatorId, out var firstCheckin) || firstCheckin.Value is null)
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

    private static decimal Clamp(decimal value)
    {
        return Math.Max(0m, Math.Min(100m, value));
    }

    private async Task<Dictionary<Guid, List<Guid>>> CollectDescendantIdsAsync(
        List<Guid> rootIds,
        CancellationToken ct)
    {
        var result = rootIds.ToDictionary(id => id, _ => new List<Guid>());
        var currentLevel = rootIds.ToList();

        while (currentLevel.Count > 0)
        {
            var children = await dbContext.Goals
                .AsNoTracking()
                .Where(g => g.ParentId.HasValue && currentLevel.Contains(g.ParentId.Value))
                .Select(g => new { g.Id, g.ParentId })
                .ToListAsync(ct);

            if (children.Count == 0)
            {
                break;
            }

            // Map each child to its root ancestor
            // Build parent-to-root mapping for current level
            var parentToRoots = new Dictionary<Guid, List<Guid>>();
            foreach (var (rootId, descendants) in result)
            {
                foreach (var descendantId in descendants.Concat([rootId]))
                {
                    if (currentLevel.Contains(descendantId))
                    {
                        if (!parentToRoots.TryGetValue(descendantId, out var roots))
                        {
                            roots = [];
                            parentToRoots[descendantId] = roots;
                        }
                        roots.Add(rootId);
                    }
                }
            }

            var nextLevel = new List<Guid>();
            foreach (var child in children)
            {
                nextLevel.Add(child.Id);
                if (parentToRoots.TryGetValue(child.ParentId!.Value, out var roots))
                {
                    foreach (var rootId in roots)
                    {
                        result[rootId].Add(child.Id);
                    }
                }
            }

            currentLevel = nextLevel;
        }

        return result;
    }
}
