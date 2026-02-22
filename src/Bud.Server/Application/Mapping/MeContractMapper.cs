using Bud.Server.Domain.ReadModels;

namespace Bud.Server.Application.Mapping;

internal static class MeContractMapper
{
    public static OrganizationSummaryResponse ToResponse(this OrganizationSummary source)
    {
        return new OrganizationSummaryResponse
        {
            Id = source.Id,
            Name = source.Name
        };
    }

    public static MyDashboardResponse ToResponse(this MyDashboardSnapshot source)
    {
        return new MyDashboardResponse
        {
            TeamHealth = source.TeamHealth.ToResponse(),
            PendingTasks = source.PendingTasks.Select(ToResponse).ToList()
        };
    }

    private static TeamHealthResponse ToResponse(this TeamHealthSnapshot source)
    {
        return new TeamHealthResponse
        {
            Leader = source.Leader?.ToResponse(),
            TeamMembers = source.TeamMembers.Select(ToResponse).ToList(),
            Engagement = source.Engagement.ToResponse(),
            Indicators = source.Indicators.ToResponse()
        };
    }

    private static DashboardLeaderResponse ToResponse(this DashboardLeaderSnapshot source)
    {
        return new DashboardLeaderResponse
        {
            Id = source.Id,
            FullName = source.FullName,
            Initials = source.Initials,
            Role = source.Role,
            TeamName = source.TeamName
        };
    }

    private static DashboardTeamMemberResponse ToResponse(this DashboardTeamMemberSnapshot source)
    {
        return new DashboardTeamMemberResponse
        {
            Id = source.Id,
            FullName = source.FullName,
            Initials = source.Initials
        };
    }

    private static EngagementScoreResponse ToResponse(this EngagementScoreSnapshot source)
    {
        return new EngagementScoreResponse
        {
            Score = source.Score,
            Level = source.Level,
            Tip = source.Tip
        };
    }

    private static TeamIndicatorsResponse ToResponse(this TeamIndicatorsSnapshot source)
    {
        return new TeamIndicatorsResponse
        {
            WeeklyAccess = source.WeeklyAccess.ToResponse(),
            MissionsUpdated = source.MissionsUpdated.ToResponse(),
            FormsResponded = source.FormsResponded.ToResponse()
        };
    }

    private static IndicatorResponse ToResponse(this IndicatorSnapshot source)
    {
        return new IndicatorResponse
        {
            Percentage = source.Percentage,
            DeltaPercentage = source.DeltaPercentage,
            IsPlaceholder = source.IsPlaceholder
        };
    }

    private static PendingTaskResponse ToResponse(this PendingTaskSnapshot source)
    {
        return new PendingTaskResponse
        {
            ReferenceId = source.ReferenceId,
            TaskType = source.TaskType,
            Title = source.Title,
            Description = source.Description,
            NavigateUrl = source.NavigateUrl
        };
    }
}
