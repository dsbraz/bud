namespace Bud.Shared.Contracts.Responses;

public sealed class MyOrganizationResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class MyDashboardResponse
{
    public MyDashboardTeamHealthResponse TeamHealth { get; set; } = new();
    public List<MyDashboardPendingTaskResponse> PendingTasks { get; set; } = [];
}

public sealed class MyDashboardTeamHealthResponse
{
    public MyDashboardLeaderResponse? Leader { get; set; }
    public List<MyDashboardTeamMemberResponse> TeamMembers { get; set; } = [];
    public MyDashboardEngagementScoreResponse Engagement { get; set; } = new();
    public MyDashboardTeamIndicatorsResponse Indicators { get; set; } = new();
}

public sealed class MyDashboardLeaderResponse
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
}

public sealed class MyDashboardTeamMemberResponse
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
}

public sealed class MyDashboardEngagementScoreResponse
{
    public int Score { get; set; }
    public string Level { get; set; } = "low";
    public string Tip { get; set; } = string.Empty;
}

public sealed class MyDashboardTeamIndicatorsResponse
{
    public MyDashboardIndicatorResponse WeeklyAccess { get; set; } = new();
    public MyDashboardIndicatorResponse MissionsUpdated { get; set; } = new();
    public MyDashboardIndicatorResponse FormsResponded { get; set; } = new();
}

public sealed class MyDashboardIndicatorResponse
{
    public int Percentage { get; set; }
    public int DeltaPercentage { get; set; }
    public bool IsPlaceholder { get; set; }
}

public sealed class MyDashboardPendingTaskResponse
{
    public Guid ReferenceId { get; set; }
    public string TaskType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string NavigateUrl { get; set; } = string.Empty;
}
