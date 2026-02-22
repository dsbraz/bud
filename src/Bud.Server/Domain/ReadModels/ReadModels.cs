using Bud.Server.Domain.Model;

namespace Bud.Server.Domain.ReadModels;

public sealed class AuthLoginResult
{
    public string Token { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsGlobalAdmin { get; set; }
    public Guid? CollaboratorId { get; set; }
    public CollaboratorRole? Role { get; set; }
    public Guid? OrganizationId { get; set; }
}

public sealed class OrganizationSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class LeaderCollaborator
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? TeamName { get; set; }
    public string? WorkspaceName { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
}

public sealed class CollaboratorHierarchyNode
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public List<CollaboratorHierarchyNode> Children { get; set; } = [];
}

public sealed class TeamSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string WorkspaceName { get; set; } = string.Empty;
}

public sealed class CollaboratorSummary
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public CollaboratorRole Role { get; set; }
}

public sealed class NotificationSummary
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ReadAtUtc { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }
}

public sealed class MissionProgressSnapshot
{
    public Guid MissionId { get; set; }
    public decimal OverallProgress { get; set; }
    public decimal ExpectedProgress { get; set; }
    public decimal AverageConfidence { get; set; }
    public int TotalMetrics { get; set; }
    public int MetricsWithCheckins { get; set; }
    public int OutdatedMetrics { get; set; }
    public List<ObjectiveProgressSnapshot> ObjectiveProgress { get; set; } = [];
}

public sealed class ObjectiveProgressSnapshot
{
    public Guid ObjectiveId { get; set; }
    public decimal OverallProgress { get; set; }
    public decimal AverageConfidence { get; set; }
    public int TotalMetrics { get; set; }
    public int MetricsWithCheckins { get; set; }
    public int OutdatedMetrics { get; set; }
}

public sealed class MetricProgressSnapshot
{
    public Guid MetricId { get; set; }
    public decimal Progress { get; set; }
    public int Confidence { get; set; }
    public bool HasCheckins { get; set; }
    public bool IsOutdated { get; set; }
    public string? LastCheckinCollaboratorName { get; set; }
}

public sealed class MyDashboardSnapshot
{
    public TeamHealthSnapshot TeamHealth { get; set; } = new();
    public List<PendingTaskSnapshot> PendingTasks { get; set; } = [];
}

public sealed class TeamHealthSnapshot
{
    public DashboardLeaderSnapshot? Leader { get; set; }
    public List<DashboardTeamMemberSnapshot> TeamMembers { get; set; } = [];
    public EngagementScoreSnapshot Engagement { get; set; } = new();
    public TeamIndicatorsSnapshot Indicators { get; set; } = new();
}

public sealed class DashboardLeaderSnapshot
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
}

public sealed class DashboardTeamMemberSnapshot
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
}

public sealed class EngagementScoreSnapshot
{
    public int Score { get; set; }
    public string Level { get; set; } = "low";
    public string Tip { get; set; } = string.Empty;
}

public sealed class TeamIndicatorsSnapshot
{
    public IndicatorSnapshot WeeklyAccess { get; set; } = new();
    public IndicatorSnapshot MissionsUpdated { get; set; } = new();
    public IndicatorSnapshot FormsResponded { get; set; } = new();
}

public sealed class IndicatorSnapshot
{
    public int Percentage { get; set; }
    public int DeltaPercentage { get; set; }
    public bool IsPlaceholder { get; set; }
}

public sealed class PendingTaskSnapshot
{
    public Guid ReferenceId { get; set; }
    public string TaskType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string NavigateUrl { get; set; } = string.Empty;
}
