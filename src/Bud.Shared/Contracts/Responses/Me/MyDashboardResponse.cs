namespace Bud.Shared.Contracts.Responses;

public sealed class MyDashboardResponse
{
    public TeamHealthResponse TeamHealth { get; set; } = new();
    public List<PendingTaskResponse> PendingTasks { get; set; } = [];
}

public sealed class TeamHealthResponse
{
    public DashboardLeaderResponse? Leader { get; set; }
    public List<DashboardTeamMemberResponse> TeamMembers { get; set; } = [];
    public EngagementScoreResponse Engagement { get; set; } = new();
    public TeamIndicatorsResponse Indicators { get; set; } = new();
}

public sealed class DashboardLeaderResponse
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
}

public sealed class DashboardTeamMemberResponse
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
}

public sealed class EngagementScoreResponse
{
    public int Score { get; set; }
    public string Level { get; set; } = "low";
    public string Tip { get; set; } = string.Empty;
}

public sealed class TeamIndicatorsResponse
{
    public IndicatorResponse WeeklyAccess { get; set; } = new();
    public IndicatorResponse MissionsUpdated { get; set; } = new();
    public IndicatorResponse FormsResponded { get; set; } = new();
}

public sealed class IndicatorResponse
{
    public int Percentage { get; set; }
    public int DeltaPercentage { get; set; }
    public bool IsPlaceholder { get; set; }
}

public sealed class PendingTaskResponse
{
    public Guid ReferenceId { get; set; }
    public string TaskType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string NavigateUrl { get; set; } = string.Empty;
}
