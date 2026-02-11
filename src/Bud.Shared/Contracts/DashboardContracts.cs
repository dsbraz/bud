namespace Bud.Shared.Contracts;

public sealed class MyDashboardResponse
{
    public TeamHealthDto TeamHealth { get; set; } = new();
    public List<PendingTaskDto> PendingTasks { get; set; } = [];
}

public sealed class TeamHealthDto
{
    public DashboardLeaderDto? Leader { get; set; }
    public List<DashboardTeamMemberDto> TeamMembers { get; set; } = [];
    public EngagementScoreDto Engagement { get; set; } = new();
    public TeamIndicatorsDto Indicators { get; set; } = new();
}

public sealed class DashboardLeaderDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
}

public sealed class DashboardTeamMemberDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
}

public sealed class EngagementScoreDto
{
    public int Score { get; set; }
    public string Level { get; set; } = "low";
    public string Tip { get; set; } = string.Empty;
}

public sealed class TeamIndicatorsDto
{
    public IndicatorDto WeeklyAccess { get; set; } = new();
    public IndicatorDto MissionsUpdated { get; set; } = new();
    public IndicatorDto FormsResponded { get; set; } = new();
}

public sealed class IndicatorDto
{
    public int Percentage { get; set; }
    public int DeltaPercentage { get; set; }
    public bool IsPlaceholder { get; set; }
}

public sealed class PendingTaskDto
{
    public Guid ReferenceId { get; set; }
    public string TaskType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string NavigateUrl { get; set; } = string.Empty;
}
