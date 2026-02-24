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
        var missionsUpdated = await CalculateMissionsUpdatedAsync(indicatorMemberIdList, organizationId, ct);
        var formsResponded = PerformanceIndicator.Placeholder();

        var avgConfidence = await CalculateAverageConfidenceAsync(indicatorMemberIdList, ct);
        var engagement = CalculateEngagement(weeklyAccess.Percentage, missionsUpdated.Percentage, avgConfidence);

        return new TeamHealthSnapshot
        {
            Leader = leader,
            TeamMembers = teamMemberDtos,
            Engagement = engagement,
            WeeklyAccess = weeklyAccess,
            MissionsUpdated = missionsUpdated,
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

    private async Task<PerformanceIndicator> CalculateMissionsUpdatedAsync(
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

        var activeMissionIds = await BuildMyActiveMissionsQuery(teamMemberIds, organizationId)
            .Select(m => m.Id)
            .ToListAsync(ct);

        if (activeMissionIds.Count == 0)
        {
            return PerformanceIndicator.Zero();
        }

        var metricIdsForActiveMissions = await dbContext.Metrics
            .AsNoTracking()
            .Where(mm => activeMissionIds.Contains(mm.MissionId))
            .Select(mm => mm.Id)
            .ToListAsync(ct);

        if (metricIdsForActiveMissions.Count == 0)
        {
            return PerformanceIndicator.Zero();
        }

        var thisWeekUpdated = await dbContext.MetricCheckins
            .AsNoTracking()
            .Where(mc => metricIdsForActiveMissions.Contains(mc.MetricId)
                && mc.CheckinDate >= thisWeekStart)
            .Select(mc => mc.Metric.MissionId)
            .Distinct()
            .CountAsync(ct);

        var lastWeekUpdated = await dbContext.MetricCheckins
            .AsNoTracking()
            .Where(mc => metricIdsForActiveMissions.Contains(mc.MetricId)
                && mc.CheckinDate >= lastWeekStart
                && mc.CheckinDate < thisWeekStart)
            .Select(mc => mc.Metric.MissionId)
            .Distinct()
            .CountAsync(ct);

        var totalActive = activeMissionIds.Count;
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

        var recentCheckins = await dbContext.MetricCheckins
            .AsNoTracking()
            .Where(mc => teamMemberIds.Contains(mc.CollaboratorId)
                && mc.CheckinDate >= DateTime.UtcNow.AddDays(-30)
                && mc.ConfidenceLevel > 0)
            .Select(mc => mc.ConfidenceLevel)
            .ToListAsync(ct);

        if (recentCheckins.Count == 0)
        {
            return 0;
        }

        var avg = recentCheckins.Average();
        return (int)Math.Round((avg / 5.0) * 100);
    }

    private static EngagementScore CalculateEngagement(int weeklyAccessPct, int missionsUpdatedPct, int confidencePct)
    {
        var score = (int)Math.Round(weeklyAccessPct * 0.30 + missionsUpdatedPct * 0.40 + confidencePct * 0.30);
        score = Math.Clamp(score, 0, 100);

        return EngagementScore.Create(score);
    }

    private IQueryable<Mission> BuildMyActiveMissionsQuery(
        List<Guid> memberIds,
        Guid organizationId)
    {
        return dbContext.Missions
            .AsNoTracking()
            .Where(m => m.Status == MissionStatus.Active
                && (memberIds.Contains(m.CollaboratorId ?? Guid.Empty)
                    || (m.TeamId != null && dbContext.CollaboratorTeams
                        .Any(ct2 => ct2.TeamId == m.TeamId && memberIds.Contains(ct2.CollaboratorId)))
                    || (m.WorkspaceId != null && dbContext.Teams
                        .Any(t => t.WorkspaceId == m.WorkspaceId && dbContext.CollaboratorTeams
                            .Any(ct2 => ct2.TeamId == t.Id && memberIds.Contains(ct2.CollaboratorId))))
                    || (m.OrganizationId == organizationId
                        && m.WorkspaceId == null && m.TeamId == null && m.CollaboratorId == null)));
    }

    private async Task<List<PendingTaskSnapshot>> BuildPendingTasksAsync(
        Collaborator collaborator,
        CancellationToken ct)
    {
        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

        var memberIds = new List<Guid> { collaborator.Id };
        var myMissions = await BuildMyActiveMissionsQuery(memberIds, collaborator.OrganizationId)
            .Include(m => m.Metrics)
                .ThenInclude(mm => mm.Checkins)
            .ToListAsync(ct);

        var tasks = new List<PendingTaskSnapshot>();

        foreach (var mission in myMissions)
        {
            var needsCheckin = mission.Metrics.Any(mm =>
                !mm.Checkins.Any(c => c.CheckinDate >= sevenDaysAgo));

            if (needsCheckin)
            {
                tasks.Add(new PendingTaskSnapshot
                {
                    ReferenceId = mission.Id,
                    TaskType = "mission_checkin",
                    Title = mission.Name,
                    Description = "Check-in pendente há mais de 7 dias",
                    NavigateUrl = $"/missions/{mission.Id}"
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
