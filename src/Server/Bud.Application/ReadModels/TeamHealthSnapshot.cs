using Bud.Domain.Model;

namespace Bud.Application.ReadModels;

public sealed class TeamHealthSnapshot
{
    public TeamLeaderSnapshot? Leader { get; set; }
    public List<TeamMemberSnapshot> TeamMembers { get; set; } = [];
    public EngagementScore Engagement { get; set; }
    public PerformanceIndicator WeeklyAccess { get; set; }
    public PerformanceIndicator MissionsUpdated { get; set; }
    public PerformanceIndicator FormsResponded { get; set; }
}
