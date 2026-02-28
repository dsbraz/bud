using Bud.Server.Application.ReadModels;
using Bud.Server.Application.Ports;
using Bud.Server.Infrastructure.Persistence;
using Bud.Server.Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace Bud.Server.Infrastructure.Repositories;

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

        if (indicatorIdsForActiveGoals.Count == 0)
        {
            return PerformanceIndicator.Zero();
        }

        var thisWeekUpdated = await dbContext.Checkins
            .AsNoTracking()
            .Where(c => indicatorIdsForActiveGoals.Contains(c.IndicatorId)
                && c.CheckinDate >= thisWeekStart)
            .Select(c => c.Indicator.GoalId)
            .Distinct()
            .CountAsync(ct);

        var lastWeekUpdated = await dbContext.Checkins
            .AsNoTracking()
            .Where(c => indicatorIdsForActiveGoals.Contains(c.IndicatorId)
                && c.CheckinDate >= lastWeekStart
                && c.CheckinDate < thisWeekStart)
            .Select(c => c.Indicator.GoalId)
            .Distinct()
            .CountAsync(ct);

        var totalActive = activeGoalIds.Count;
        var currentPct = (int)Math.Round(thisWeekUpdated * 100.0 / totalActive);
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
                    || (g.TeamId != null && dbContext.CollaboratorTeams
                        .Any(ct2 => ct2.TeamId == g.TeamId && memberIds.Contains(ct2.CollaboratorId)))
                    || (g.WorkspaceId != null && dbContext.Teams
                        .Any(t => t.WorkspaceId == g.WorkspaceId && dbContext.CollaboratorTeams
                            .Any(ct2 => ct2.TeamId == t.Id && memberIds.Contains(ct2.CollaboratorId))))
                    || (g.OrganizationId == organizationId
                        && g.WorkspaceId == null && g.TeamId == null && g.CollaboratorId == null)));
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

        return tasks;
    }

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
