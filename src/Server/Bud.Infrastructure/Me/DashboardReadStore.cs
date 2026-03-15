using Bud.Infrastructure.Persistence;
using Bud.Shared.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Bud.Infrastructure.Me;

public sealed class DashboardReadStore(ApplicationDbContext dbContext) : IMyDashboardReadStore
{
    public async Task<DashboardSnapshot?> GetMyDashboardAsync(
        Guid collaboratorId,
        Guid? teamId,
        CancellationToken ct = default)
    {
        var collaborator = await dbContext.Collaborators
            .AsNoTracking()
            .Include(c => c.Team)
            .Include(c => c.Leader)
                .ThenInclude(l => l!.Team)
            .FirstOrDefaultAsync(c => c.Id == collaboratorId, ct);

        if (collaborator is null)
        {
            return null;
        }

        Collaborator? leaderSource;
        List<Collaborator> teamMembers;
        string? teamNameOverride = null;

        if (teamId.HasValue)
        {
            var team = await dbContext.Teams
                .AsNoTracking()
                .Include(t => t.Leader)
                .FirstOrDefaultAsync(t => t.Id == teamId.Value, ct);

            leaderSource = team?.Leader;
            teamNameOverride = team?.Name;

            var memberIds = await dbContext.CollaboratorTeams
                .AsNoTracking()
                .Where(cteam => cteam.TeamId == teamId.Value)
                .Select(cteam => cteam.CollaboratorId)
                .ToListAsync(ct);

            teamMembers = memberIds.Count > 0
                ? await dbContext.Collaborators
                    .AsNoTracking()
                    .Where(c => memberIds.Contains(c.Id))
                    .ToListAsync(ct)
                : [];
        }
        else
        {
            var primaryTeamId = collaborator.TeamId;

            if (primaryTeamId.HasValue)
            {
                var team = await dbContext.Teams
                    .AsNoTracking()
                    .Include(t => t.Leader)
                    .FirstOrDefaultAsync(t => t.Id == primaryTeamId.Value, ct);

                leaderSource = team?.Leader;
                teamNameOverride = team?.Name;

                var memberIds = await dbContext.CollaboratorTeams
                    .AsNoTracking()
                    .Where(cteam => cteam.TeamId == primaryTeamId.Value)
                    .Select(cteam => cteam.CollaboratorId)
                    .ToListAsync(ct);

                teamMembers = memberIds.Count > 0
                    ? await dbContext.Collaborators
                        .AsNoTracking()
                        .Where(c => memberIds.Contains(c.Id))
                        .ToListAsync(ct)
                    : [];
            }
            else
            {
                leaderSource = collaborator.Leader
                    ?? (collaborator.Role == CollaboratorRole.Leader ? collaborator : null);
                teamMembers = [];
            }
        }

        var teamHealth = await BuildTeamHealthAsync(leaderSource, teamMembers, collaborator.Id, collaborator.OrganizationId, teamNameOverride, ct);
        var pendingTasks = await BuildPendingTasksAsync(collaborator, ct);

        return new DashboardSnapshot
        {
            TeamHealth = teamHealth,
            PendingTasks = pendingTasks
        };
    }

    private async Task<TeamHealthSnapshot> BuildTeamHealthAsync(
        Collaborator? leaderSource,
        List<Collaborator> directReports,
        Guid currentCollaboratorId,
        Guid organizationId,
        string? teamNameOverride,
        CancellationToken ct)
    {
        var leader = BuildLeaderDto(leaderSource, teamNameOverride);
        var teamMemberDtos = BuildTeamMembers(directReports);
        var teamMemberIds = directReports.Select(m => m.Id).ToList();

        // Indicadores sempre incluem o próprio usuário
        var indicatorMemberIds = new HashSet<Guid>(teamMemberIds) { currentCollaboratorId };
        var indicatorMemberIdList = indicatorMemberIds.ToList();

        var weeklyAccess = await CalculateWeeklyAccessAsync(indicatorMemberIdList, organizationId, ct);
        var goalsUpdated = await CalculateGoalsUpdatedAsync(indicatorMemberIdList, organizationId, ct);
        var formsResponded = PerformanceIndicator.Placeholder();

        var avgConfidence = await CalculateAverageConfidenceAsync(indicatorMemberIdList, ct);
        var engagement = CalculateEngagement(weeklyAccess.Percentage, goalsUpdated.Percentage, avgConfidence);

        return new TeamHealthSnapshot
        {
            Leader = leader,
            TeamMembers = teamMemberDtos,
            Engagement = engagement,
            WeeklyAccess = weeklyAccess,
            MissionsUpdated = goalsUpdated,
            FormsResponded = formsResponded
        };
    }

    private static TeamLeaderSnapshot? BuildLeaderDto(Collaborator? leaderSource, string? teamNameOverride = null)
    {
        if (leaderSource is null)
        {
            return null;
        }

        return new TeamLeaderSnapshot
        {
            Id = leaderSource.Id,
            FullName = leaderSource.FullName,
            Initials = GetInitials(leaderSource.FullName),
            Role = leaderSource.Role == CollaboratorRole.Leader ? "Líder" : "Colaborador",
            TeamName = teamNameOverride ?? leaderSource.Team?.Name ?? string.Empty
        };
    }

    private static List<TeamMemberSnapshot> BuildTeamMembers(List<Collaborator> members)
    {
        return members.Select(m => new TeamMemberSnapshot
        {
            Id = m.Id,
            FullName = m.FullName,
            Initials = GetInitials(m.FullName)
        }).ToList();
    }

    private async Task<PerformanceIndicator> CalculateWeeklyAccessAsync(
        List<Guid> teamMemberIds,
        Guid organizationId,
        CancellationToken ct)
    {
        if (teamMemberIds.Count == 0)
        {
            return PerformanceIndicator.Zero();
        }

        var now = DateTime.UtcNow;
        var thisWeekStart = now.AddDays(-7);
        var lastWeekStart = now.AddDays(-14);

        var thisWeekCount = await dbContext.CollaboratorAccessLogs
            .AsNoTracking()
            .Where(l => l.OrganizationId == organizationId
                && teamMemberIds.Contains(l.CollaboratorId)
                && l.AccessedAt >= thisWeekStart)
            .Select(l => l.CollaboratorId)
            .Distinct()
            .CountAsync(ct);

        var lastWeekCount = await dbContext.CollaboratorAccessLogs
            .AsNoTracking()
            .Where(l => l.OrganizationId == organizationId
                && teamMemberIds.Contains(l.CollaboratorId)
                && l.AccessedAt >= lastWeekStart
                && l.AccessedAt < thisWeekStart)
            .Select(l => l.CollaboratorId)
            .Distinct()
            .CountAsync(ct);

        var total = teamMemberIds.Count;
        var currentPct = (int)Math.Round(thisWeekCount * 100.0 / total);
        var previousPct = (int)Math.Round(lastWeekCount * 100.0 / total);

        return PerformanceIndicator.Create(currentPct, currentPct - previousPct);
    }

    private async Task<PerformanceIndicator> CalculateGoalsUpdatedAsync(
        List<Guid> teamMemberIds,
        Guid organizationId,
        CancellationToken ct)
    {
        if (teamMemberIds.Count == 0)
        {
            return PerformanceIndicator.Zero();
        }

        var now = DateTime.UtcNow;
        var thisWeekStart = now.AddDays(-7);
        var lastWeekStart = now.AddDays(-14);

        var activeGoalIds = await BuildMyActiveGoalsQuery(teamMemberIds, organizationId)
            .Select(g => g.Id)
            .ToListAsync(ct);

        if (activeGoalIds.Count == 0)
        {
            return PerformanceIndicator.Zero();
        }

        var indicatorIdsForActiveGoals = await dbContext.Indicators
            .AsNoTracking()
            .Where(i => activeGoalIds.Contains(i.GoalId))
            .Select(i => i.Id)
            .ToListAsync(ct);

        // Goals updated via checkins this week
        var thisWeekUpdatedViaCheckins = indicatorIdsForActiveGoals.Count > 0
            ? await dbContext.Checkins
                .AsNoTracking()
                .Where(c => indicatorIdsForActiveGoals.Contains(c.IndicatorId)
                    && c.CheckinDate >= thisWeekStart)
                .Select(c => c.Indicator.GoalId)
                .Distinct()
                .ToListAsync(ct)
            : new List<Guid>();

        // Goals updated via Doing tasks (considered "atualizada")
        var goalsWithDoingTasks = await dbContext.GoalTasks
            .AsNoTracking()
            .Where(t => activeGoalIds.Contains(t.GoalId) && t.State == TaskState.Doing)
            .Select(t => t.GoalId)
            .Distinct()
            .ToListAsync(ct);

        var thisWeekUpdatedGoalIds = thisWeekUpdatedViaCheckins
            .Union(goalsWithDoingTasks)
            .Intersect(activeGoalIds)
            .ToHashSet();

        // Last week: only checkin-based (tasks are a current snapshot, not historical)
        var lastWeekUpdated = indicatorIdsForActiveGoals.Count > 0
            ? await dbContext.Checkins
                .AsNoTracking()
                .Where(c => indicatorIdsForActiveGoals.Contains(c.IndicatorId)
                    && c.CheckinDate >= lastWeekStart
                    && c.CheckinDate < thisWeekStart)
                .Select(c => c.Indicator.GoalId)
                .Distinct()
                .CountAsync(ct)
            : 0;

        var totalActive = activeGoalIds.Count;
        if (totalActive == 0)
        {
            return PerformanceIndicator.Zero();
        }

        var currentPct = (int)Math.Round(thisWeekUpdatedGoalIds.Count * 100.0 / totalActive);
        var previousPct = (int)Math.Round(lastWeekUpdated * 100.0 / totalActive);

        return PerformanceIndicator.Create(currentPct, currentPct - previousPct);
    }

    private async Task<int> CalculateAverageConfidenceAsync(
        List<Guid> teamMemberIds,
        CancellationToken ct)
    {
        if (teamMemberIds.Count == 0)
        {
            return 0;
        }

        var recentCheckins = await dbContext.Checkins
            .AsNoTracking()
            .Where(c => teamMemberIds.Contains(c.CollaboratorId)
                && c.CheckinDate >= DateTime.UtcNow.AddDays(-30)
                && c.ConfidenceLevel > 0)
            .Select(c => c.ConfidenceLevel)
            .ToListAsync(ct);

        if (recentCheckins.Count == 0)
        {
            return 0;
        }

        var avg = recentCheckins.Average();
        return (int)Math.Round((avg / 5.0) * 100);
    }

    private static EngagementScore CalculateEngagement(int weeklyAccessPct, int goalsUpdatedPct, int confidencePct)
    {
        var score = (int)Math.Round(weeklyAccessPct * 0.30 + goalsUpdatedPct * 0.40 + confidencePct * 0.30);
        score = Math.Clamp(score, 0, 100);

        return EngagementScore.Create(score);
    }

    private IQueryable<Goal> BuildMyActiveGoalsQuery(
        List<Guid> memberIds,
        Guid organizationId)
    {
        return dbContext.Goals
            .AsNoTracking()
            .Where(g => g.Status == GoalStatus.Active
                && (memberIds.Contains(g.CollaboratorId ?? Guid.Empty)
                    || (g.OrganizationId == organizationId && g.CollaboratorId == null)));
    }

    private async Task<List<PendingTaskSnapshot>> BuildPendingTasksAsync(
        Collaborator collaborator,
        CancellationToken ct)
    {
        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

        var memberIds = new List<Guid> { collaborator.Id };
        var myGoals = await BuildMyActiveGoalsQuery(memberIds, collaborator.OrganizationId)
            .Include(g => g.Indicators)
                .ThenInclude(i => i.Checkins)
            .ToListAsync(ct);

        var tasks = new List<PendingTaskSnapshot>();

        foreach (var goal in myGoals)
        {
            var needsCheckin = goal.Indicators.Any(i =>
                !i.Checkins.Any(c => c.CheckinDate >= sevenDaysAgo));

            if (needsCheckin)
            {
                tasks.Add(new PendingTaskSnapshot
                {
                    ReferenceId = goal.Id,
                    TaskType = "goal_checkin",
                    Title = goal.Name,
                    Description = "Check-in pendente há mais de 7 dias",
                    NavigateUrl = $"/goals/{goal.Id}"
                });
            }
        }

        // Add active GoalTasks (ToDo and Doing) from accessible goals
        var myGoalIds = myGoals.Select(g => g.Id).ToList();
        if (myGoalIds.Count > 0)
        {
            var activeTasks = await dbContext.GoalTasks
                .AsNoTracking()
                .Where(t => myGoalIds.Contains(t.GoalId)
                    && (t.State == TaskState.ToDo || t.State == TaskState.Doing))
                .ToListAsync(ct);

            var goalNameById = myGoals.ToDictionary(g => g.Id, g => g.Name);
            foreach (var goalTask in activeTasks)
            {
                var goalName = goalNameById.GetValueOrDefault(goalTask.GoalId, string.Empty);
                tasks.Add(new PendingTaskSnapshot
                {
                    ReferenceId = goalTask.Id,
                    TaskType = "goal_task",
                    Title = goalTask.Name,
                    Description = $"Meta: {goalName} — {GetTaskStateLabel(goalTask.State)}",
                    NavigateUrl = $"/goals/{goalTask.GoalId}"
                });
            }
        }

        return tasks;
    }

    private static string GetTaskStateLabel(TaskState state) => state switch
    {
        TaskState.ToDo => "A fazer",
        TaskState.Doing => "Em andamento",
        _ => string.Empty
    };

    private static string GetInitials(string fullName)
    {
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return "?";
        }

        if (parts.Length == 1)
        {
            return parts[0][..1].ToUpperInvariant();
        }

        return $"{parts[0][..1]}{parts[^1][..1]}".ToUpperInvariant();
    }
}
