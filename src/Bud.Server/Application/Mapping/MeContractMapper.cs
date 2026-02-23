using Bud.Server.Application.ReadModels;
using Bud.Server.Domain.Model;

namespace Bud.Server.Application.Mapping;

internal static class MeContractMapper
{
    public static MyOrganizationResponse ToResponse(this OrganizationSnapshot source)
    {
        return new MyOrganizationResponse
        {
            Id = source.Id,
            Name = source.Name
        };
    }

    public static MyDashboardResponse ToResponse(this DashboardSnapshot source)
    {
        return new MyDashboardResponse
        {
            TeamHealth = source.TeamHealth.ToResponse(),
            PendingTasks = source.PendingTasks.Select(ToResponse).ToList()
        };
    }

    private static MyDashboardTeamHealthResponse ToResponse(this TeamHealthSnapshot source)
    {
        return new MyDashboardTeamHealthResponse
        {
            Leader = source.Leader?.ToResponse(),
            TeamMembers = source.TeamMembers.Select(ToResponse).ToList(),
            Engagement = source.Engagement.ToResponse(),
            Indicators = new MyDashboardTeamIndicatorsResponse
            {
                WeeklyAccess = source.WeeklyAccess.ToResponse(),
                MissionsUpdated = source.MissionsUpdated.ToResponse(),
                FormsResponded = source.FormsResponded.ToResponse()
            }
        };
    }

    private static MyDashboardLeaderResponse ToResponse(this TeamLeaderSnapshot source)
    {
        return new MyDashboardLeaderResponse
        {
            Id = source.Id,
            FullName = source.FullName,
            Initials = source.Initials,
            Role = source.Role,
            TeamName = source.TeamName
        };
    }

    private static MyDashboardTeamMemberResponse ToResponse(this TeamMemberSnapshot source)
    {
        return new MyDashboardTeamMemberResponse
        {
            Id = source.Id,
            FullName = source.FullName,
            Initials = source.Initials
        };
    }

    private static MyDashboardEngagementScoreResponse ToResponse(this EngagementScore source)
    {
        return new MyDashboardEngagementScoreResponse
        {
            Score = source.Score,
            Level = source.Level,
            Tip = source.Tip
        };
    }

    private static MyDashboardIndicatorResponse ToResponse(this PerformanceIndicator source)
    {
        return new MyDashboardIndicatorResponse
        {
            Percentage = source.Percentage,
            DeltaPercentage = source.DeltaPercentage,
            IsPlaceholder = source.IsPlaceholder
        };
    }

    private static MyDashboardPendingTaskResponse ToResponse(this PendingTaskSnapshot source)
    {
        return new MyDashboardPendingTaskResponse
        {
            ReferenceId = source.ReferenceId,
            TaskType = source.TaskType,
            Title = source.Title,
            Description = source.Description,
            NavigateUrl = source.NavigateUrl
        };
    }
}
