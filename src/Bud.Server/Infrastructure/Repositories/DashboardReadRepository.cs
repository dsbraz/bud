using Bud.Server.Domain.ReadModels;
using Bud.Server.Application.Me;
using Bud.Server.Infrastructure.Persistence;
using Bud.Server.Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace Bud.Server.Infrastructure.Repositories;

public sealed class DashboardReadRepository(ApplicationDbContext dbContext) : IMyDashboardReadStore
{
    public async Task<MyDashboardSnapshot?> GetMyDashboardAsync(
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
            leaderSource = collaborator.Leader
                ?? (collaborator.Role == CollaboratorRole.Leader ? collaborator : null);

            teamMembers = leaderSource is not null
                ? await dbContext.Collaborators
                    .AsNoTracking()
                    .Where(c => c.LeaderId == leaderSource.Id)
                    .ToListAsync(ct)
                : [];
        }

        var teamHealth = await BuildTeamHealthAsync(leaderSource, teamMembers, collaborator.OrganizationId, teamNameOverride, ct);
        var pendingTasks = await BuildPendingTasksAsync(collaborator, ct);

        return new MyDashboardSnapshot
        {
            TeamHealth = teamHealth,
            PendingTasks = pendingTasks
        };
    }

    private async Task<TeamHealthSnapshot> BuildTeamHealthAsync(
        Collaborator? leaderSource,
        List<Collaborator> directReports,
        Guid organizationId,
        string? teamNameOverride,
        CancellationToken ct)
    {
        var leader = BuildLeaderDto(leaderSource, teamNameOverride);
        var teamMemberDtos = BuildTeamMembers(directReports);
        var teamMemberIds = directReports.Select(m => m.Id).ToList();

        var weeklyAccess = await CalculateWeeklyAccessAsync(teamMemberIds, organizationId, ct);
        var missionsUpdated = await CalculateMissionsUpdatedAsync(teamMemberIds, ct);
        var formsResponded = new IndicatorSnapshot { Percentage = 0, DeltaPercentage = 0, IsPlaceholder = true };

        var avgConfidence = await CalculateAverageConfidenceAsync(teamMemberIds, ct);
        var engagement = CalculateEngagement(weeklyAccess.Percentage, missionsUpdated.Percentage, avgConfidence);

        return new TeamHealthSnapshot
        {
            Leader = leader,
            TeamMembers = teamMemberDtos,
            Engagement = engagement,
            Indicators = new TeamIndicatorsSnapshot
            {
                WeeklyAccess = weeklyAccess,
                MissionsUpdated = missionsUpdated,
                FormsResponded = formsResponded
            }
        };
    }

    private static DashboardLeaderSnapshot? BuildLeaderDto(Collaborator? leaderSource, string? teamNameOverride = null)
    {
        if (leaderSource is null)
        {
            return null;
        }

        return new DashboardLeaderSnapshot
        {
            Id = leaderSource.Id,
            FullName = leaderSource.FullName,
            Initials = GetInitials(leaderSource.FullName),
            Role = leaderSource.Role == CollaboratorRole.Leader ? "Líder" : "Colaborador",
            TeamName = teamNameOverride ?? leaderSource.Team?.Name ?? string.Empty
        };
    }

    private static List<DashboardTeamMemberSnapshot> BuildTeamMembers(List<Collaborator> members)
    {
        return members.Select(m => new DashboardTeamMemberSnapshot
        {
            Id = m.Id,
            FullName = m.FullName,
            Initials = GetInitials(m.FullName)
        }).ToList();
    }

    private async Task<IndicatorSnapshot> CalculateWeeklyAccessAsync(
        List<Guid> teamMemberIds,
        Guid organizationId,
        CancellationToken ct)
    {
        if (teamMemberIds.Count == 0)
        {
            return new IndicatorSnapshot { Percentage = 0, DeltaPercentage = 0 };
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

        return new IndicatorSnapshot
        {
            Percentage = currentPct,
            DeltaPercentage = currentPct - previousPct
        };
    }

    private async Task<IndicatorSnapshot> CalculateMissionsUpdatedAsync(
        List<Guid> teamMemberIds,
        CancellationToken ct)
    {
        if (teamMemberIds.Count == 0)
        {
            return new IndicatorSnapshot { Percentage = 0, DeltaPercentage = 0 };
        }

        var now = DateTime.UtcNow;
        var thisWeekStart = now.AddDays(-7);
        var lastWeekStart = now.AddDays(-14);

        var activeMissionIds = await dbContext.Missions
            .AsNoTracking()
            .Where(m => m.Status == MissionStatus.Active
                && (teamMemberIds.Contains(m.CollaboratorId ?? Guid.Empty)
                    || (m.TeamId != null && dbContext.Collaborators
                        .Any(c => c.TeamId == m.TeamId && teamMemberIds.Contains(c.Id)))))
            .Select(m => m.Id)
            .ToListAsync(ct);

        if (activeMissionIds.Count == 0)
        {
            return new IndicatorSnapshot { Percentage = 0, DeltaPercentage = 0 };
        }

        var metricIdsForActiveMissions = await dbContext.MissionMetrics
            .AsNoTracking()
            .Where(mm => activeMissionIds.Contains(mm.MissionId))
            .Select(mm => mm.Id)
            .ToListAsync(ct);

        if (metricIdsForActiveMissions.Count == 0)
        {
            return new IndicatorSnapshot { Percentage = 0, DeltaPercentage = 0 };
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

        return new IndicatorSnapshot
        {
            Percentage = currentPct,
            DeltaPercentage = currentPct - previousPct
        };
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

    private static EngagementScoreSnapshot CalculateEngagement(int weeklyAccessPct, int missionsUpdatedPct, int confidencePct)
    {
        var score = (int)Math.Round(weeklyAccessPct * 0.30 + missionsUpdatedPct * 0.40 + confidencePct * 0.30);
        score = Math.Clamp(score, 0, 100);

        var level = score >= 70 ? "high" : score >= 40 ? "medium" : "low";

        var tip = level switch
        {
            "high" => "Excelente! Seu time está engajado e acompanhando as missões de perto.",
            "medium" => "Bom progresso! Incentive o time a manter a frequência de check-ins.",
            _ => "Atenção: o engajamento está baixo. Considere alinhar prioridades com o time."
        };

        return new EngagementScoreSnapshot
        {
            Score = score,
            Level = level,
            Tip = tip
        };
    }

    private async Task<List<PendingTaskSnapshot>> BuildPendingTasksAsync(
        Collaborator collaborator,
        CancellationToken ct)
    {
        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

        var myMissions = await dbContext.Missions
            .AsNoTracking()
            .Include(m => m.Metrics)
                .ThenInclude(mm => mm.Checkins)
            .Where(m => m.Status == MissionStatus.Active && m.CollaboratorId == collaborator.Id)
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
