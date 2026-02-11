using Bud.Server.Data;
using Bud.Shared.Contracts;
using Bud.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Bud.Server.Services;

public sealed class DashboardService(ApplicationDbContext dbContext) : IDashboardService
{
    public async Task<ServiceResult<MyDashboardResponse>> GetMyDashboardAsync(
        Guid collaboratorId,
        CancellationToken cancellationToken = default)
    {
        var collaborator = await dbContext.Collaborators
            .AsNoTracking()
            .Include(c => c.Team)
            .Include(c => c.Leader)
                .ThenInclude(l => l!.Team)
            .FirstOrDefaultAsync(c => c.Id == collaboratorId, cancellationToken);

        if (collaborator is null)
        {
            return ServiceResult<MyDashboardResponse>.NotFound("Colaborador não encontrado.");
        }

        // Determine the leader: explicit leader or self if role is Leader
        var leaderSource = collaborator.Leader
            ?? (collaborator.Role == CollaboratorRole.Leader ? collaborator : null);

        // Fetch direct reports of the leader (collaborators whose LeaderId points to the leader)
        var directReports = leaderSource is not null
            ? await dbContext.Collaborators
                .AsNoTracking()
                .Where(c => c.LeaderId == leaderSource.Id)
                .ToListAsync(cancellationToken)
            : [];

        var teamHealth = await BuildTeamHealthAsync(leaderSource, directReports, collaborator.OrganizationId, cancellationToken);
        var pendingTasks = await BuildPendingTasksAsync(collaborator, cancellationToken);

        return ServiceResult<MyDashboardResponse>.Success(new MyDashboardResponse
        {
            TeamHealth = teamHealth,
            PendingTasks = pendingTasks
        });
    }

    private async Task<TeamHealthDto> BuildTeamHealthAsync(
        Collaborator? leaderSource,
        List<Collaborator> directReports,
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        var leader = BuildLeaderDto(leaderSource);
        var teamMembers = BuildTeamMembers(directReports);
        var teamMemberIds = directReports.Select(m => m.Id).ToList();

        var weeklyAccess = await CalculateWeeklyAccessAsync(teamMemberIds, organizationId, cancellationToken);
        var missionsUpdated = await CalculateMissionsUpdatedAsync(teamMemberIds, cancellationToken);
        var formsResponded = new IndicatorDto { Percentage = 0, DeltaPercentage = 0, IsPlaceholder = true };

        var avgConfidence = await CalculateAverageConfidenceAsync(teamMemberIds, cancellationToken);
        var engagement = CalculateEngagement(weeklyAccess.Percentage, missionsUpdated.Percentage, avgConfidence);

        return new TeamHealthDto
        {
            Leader = leader,
            TeamMembers = teamMembers,
            Engagement = engagement,
            Indicators = new TeamIndicatorsDto
            {
                WeeklyAccess = weeklyAccess,
                MissionsUpdated = missionsUpdated,
                FormsResponded = formsResponded
            }
        };
    }

    private static DashboardLeaderDto? BuildLeaderDto(Collaborator? leaderSource)
    {
        if (leaderSource is null)
        {
            return null;
        }

        return new DashboardLeaderDto
        {
            Id = leaderSource.Id,
            FullName = leaderSource.FullName,
            Initials = GetInitials(leaderSource.FullName),
            Role = leaderSource.Role == CollaboratorRole.Leader ? "Líder" : "Colaborador",
            TeamName = leaderSource.Team?.Name ?? string.Empty
        };
    }

    private static List<DashboardTeamMemberDto> BuildTeamMembers(List<Collaborator> members)
    {
        return members.Select(m => new DashboardTeamMemberDto
        {
            Id = m.Id,
            FullName = m.FullName,
            Initials = GetInitials(m.FullName)
        }).ToList();
    }

    private async Task<IndicatorDto> CalculateWeeklyAccessAsync(
        List<Guid> teamMemberIds,
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        if (teamMemberIds.Count == 0)
        {
            return new IndicatorDto { Percentage = 0, DeltaPercentage = 0 };
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
            .CountAsync(cancellationToken);

        var lastWeekCount = await dbContext.CollaboratorAccessLogs
            .AsNoTracking()
            .Where(l => l.OrganizationId == organizationId
                && teamMemberIds.Contains(l.CollaboratorId)
                && l.AccessedAt >= lastWeekStart
                && l.AccessedAt < thisWeekStart)
            .Select(l => l.CollaboratorId)
            .Distinct()
            .CountAsync(cancellationToken);

        var total = teamMemberIds.Count;
        var currentPct = (int)Math.Round(thisWeekCount * 100.0 / total);
        var previousPct = (int)Math.Round(lastWeekCount * 100.0 / total);

        return new IndicatorDto
        {
            Percentage = currentPct,
            DeltaPercentage = currentPct - previousPct
        };
    }

    private async Task<IndicatorDto> CalculateMissionsUpdatedAsync(
        List<Guid> teamMemberIds,
        CancellationToken cancellationToken)
    {
        if (teamMemberIds.Count == 0)
        {
            return new IndicatorDto { Percentage = 0, DeltaPercentage = 0 };
        }

        var now = DateTime.UtcNow;
        var thisWeekStart = now.AddDays(-7);
        var lastWeekStart = now.AddDays(-14);

        // Active missions for the team members (personal scope or team scope)
        var activeMissionIds = await dbContext.Missions
            .AsNoTracking()
            .Where(m => m.Status == MissionStatus.Active
                && (teamMemberIds.Contains(m.CollaboratorId ?? Guid.Empty)
                    || (m.TeamId != null && dbContext.Collaborators
                        .Any(c => c.TeamId == m.TeamId && teamMemberIds.Contains(c.Id)))))
            .Select(m => m.Id)
            .ToListAsync(cancellationToken);

        if (activeMissionIds.Count == 0)
        {
            return new IndicatorDto { Percentage = 0, DeltaPercentage = 0 };
        }

        var metricIdsForActiveMissions = await dbContext.MissionMetrics
            .AsNoTracking()
            .Where(mm => activeMissionIds.Contains(mm.MissionId))
            .Select(mm => mm.Id)
            .ToListAsync(cancellationToken);

        if (metricIdsForActiveMissions.Count == 0)
        {
            return new IndicatorDto { Percentage = 0, DeltaPercentage = 0 };
        }

        // Missions with at least one checkin this week
        var thisWeekUpdated = await dbContext.MetricCheckins
            .AsNoTracking()
            .Where(mc => metricIdsForActiveMissions.Contains(mc.MissionMetricId)
                && mc.CheckinDate >= thisWeekStart)
            .Select(mc => mc.MissionMetric.MissionId)
            .Distinct()
            .CountAsync(cancellationToken);

        var lastWeekUpdated = await dbContext.MetricCheckins
            .AsNoTracking()
            .Where(mc => metricIdsForActiveMissions.Contains(mc.MissionMetricId)
                && mc.CheckinDate >= lastWeekStart
                && mc.CheckinDate < thisWeekStart)
            .Select(mc => mc.MissionMetric.MissionId)
            .Distinct()
            .CountAsync(cancellationToken);

        var totalActive = activeMissionIds.Count;
        var currentPct = (int)Math.Round(thisWeekUpdated * 100.0 / totalActive);
        var previousPct = (int)Math.Round(lastWeekUpdated * 100.0 / totalActive);

        return new IndicatorDto
        {
            Percentage = currentPct,
            DeltaPercentage = currentPct - previousPct
        };
    }

    private async Task<int> CalculateAverageConfidenceAsync(
        List<Guid> teamMemberIds,
        CancellationToken cancellationToken)
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
            .ToListAsync(cancellationToken);

        if (recentCheckins.Count == 0)
        {
            return 0;
        }

        // ConfidenceLevel is 1-5, normalize to 0-100
        var avg = recentCheckins.Average();
        return (int)Math.Round((avg / 5.0) * 100);
    }

    private static EngagementScoreDto CalculateEngagement(int weeklyAccessPct, int missionsUpdatedPct, int confidencePct)
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

        return new EngagementScoreDto
        {
            Score = score,
            Level = level,
            Tip = tip
        };
    }

    private async Task<List<PendingTaskDto>> BuildPendingTasksAsync(
        Collaborator collaborator,
        CancellationToken cancellationToken)
    {
        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

        // Active missions for this collaborator (personal scope)
        var myMissions = await dbContext.Missions
            .AsNoTracking()
            .Include(m => m.Metrics)
                .ThenInclude(mm => mm.Checkins)
            .Where(m => m.Status == MissionStatus.Active && m.CollaboratorId == collaborator.Id)
            .ToListAsync(cancellationToken);

        var tasks = new List<PendingTaskDto>();

        foreach (var mission in myMissions)
        {
            // Check if any metric has no recent checkin
            var needsCheckin = mission.Metrics.Any(mm =>
                !mm.Checkins.Any(c => c.CheckinDate >= sevenDaysAgo));

            if (needsCheckin)
            {
                tasks.Add(new PendingTaskDto
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
