using Bud.Server.Domain.ReadModels;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.Common;

internal static class ContractMappings
{
    public static AuthLoginResponse ToContract(this AuthLoginResult source)
    {
        return new AuthLoginResponse
        {
            Token = source.Token,
            Email = source.Email,
            DisplayName = source.DisplayName,
            IsGlobalAdmin = source.IsGlobalAdmin,
            CollaboratorId = source.CollaboratorId,
            Role = source.Role,
            OrganizationId = source.OrganizationId
        };
    }

    public static OrganizationSummaryDto ToContract(this OrganizationSummary source)
    {
        return new OrganizationSummaryDto
        {
            Id = source.Id,
            Name = source.Name
        };
    }

    public static NotificationDto ToContract(this NotificationSummary source)
    {
        return new NotificationDto
        {
            Id = source.Id,
            Title = source.Title,
            Message = source.Message,
            Type = source.Type,
            IsRead = source.IsRead,
            CreatedAtUtc = source.CreatedAtUtc,
            ReadAtUtc = source.ReadAtUtc,
            RelatedEntityId = source.RelatedEntityId,
            RelatedEntityType = source.RelatedEntityType
        };
    }

    public static CollaboratorSummaryDto ToContract(this CollaboratorSummary source)
    {
        return new CollaboratorSummaryDto
        {
            Id = source.Id,
            FullName = source.FullName,
            Email = source.Email,
            Role = source.Role
        };
    }

    public static TeamSummaryDto ToContract(this TeamSummary source)
    {
        return new TeamSummaryDto
        {
            Id = source.Id,
            Name = source.Name,
            WorkspaceName = source.WorkspaceName
        };
    }

    public static LeaderCollaboratorResponse ToContract(this LeaderCollaborator source)
    {
        return new LeaderCollaboratorResponse
        {
            Id = source.Id,
            FullName = source.FullName,
            Email = source.Email,
            TeamName = source.TeamName,
            WorkspaceName = source.WorkspaceName,
            OrganizationName = source.OrganizationName
        };
    }

    public static CollaboratorHierarchyNodeDto ToContract(this CollaboratorHierarchyNode source)
    {
        return new CollaboratorHierarchyNodeDto
        {
            Id = source.Id,
            FullName = source.FullName,
            Initials = source.Initials,
            Role = source.Role,
            Children = source.Children.Select(ToContract).ToList()
        };
    }

    public static MissionProgressDto ToContract(this MissionProgressSnapshot source)
    {
        return new MissionProgressDto
        {
            MissionId = source.MissionId,
            OverallProgress = source.OverallProgress,
            ExpectedProgress = source.ExpectedProgress,
            AverageConfidence = source.AverageConfidence,
            TotalMetrics = source.TotalMetrics,
            MetricsWithCheckins = source.MetricsWithCheckins,
            OutdatedMetrics = source.OutdatedMetrics,
            ObjectiveProgress = source.ObjectiveProgress.Select(ToContract).ToList()
        };
    }

    public static ObjectiveProgressDto ToContract(this ObjectiveProgressSnapshot source)
    {
        return new ObjectiveProgressDto
        {
            ObjectiveId = source.ObjectiveId,
            OverallProgress = source.OverallProgress,
            AverageConfidence = source.AverageConfidence,
            TotalMetrics = source.TotalMetrics,
            MetricsWithCheckins = source.MetricsWithCheckins,
            OutdatedMetrics = source.OutdatedMetrics
        };
    }

    public static MetricProgressDto ToContract(this MetricProgressSnapshot source)
    {
        return new MetricProgressDto
        {
            MetricId = source.MetricId,
            Progress = source.Progress,
            Confidence = source.Confidence,
            HasCheckins = source.HasCheckins,
            IsOutdated = source.IsOutdated,
            LastCheckinCollaboratorName = source.LastCheckinCollaboratorName
        };
    }

    public static MyDashboardResponse ToContract(this MyDashboardSnapshot source)
    {
        return new MyDashboardResponse
        {
            TeamHealth = source.TeamHealth.ToContract(),
            PendingTasks = source.PendingTasks.Select(ToContract).ToList()
        };
    }

    private static TeamHealthDto ToContract(this TeamHealthSnapshot source)
    {
        return new TeamHealthDto
        {
            Leader = source.Leader?.ToContract(),
            TeamMembers = source.TeamMembers.Select(ToContract).ToList(),
            Engagement = source.Engagement.ToContract(),
            Indicators = source.Indicators.ToContract()
        };
    }

    private static DashboardLeaderDto ToContract(this DashboardLeaderSnapshot source)
    {
        return new DashboardLeaderDto
        {
            Id = source.Id,
            FullName = source.FullName,
            Initials = source.Initials,
            Role = source.Role,
            TeamName = source.TeamName
        };
    }

    private static DashboardTeamMemberDto ToContract(this DashboardTeamMemberSnapshot source)
    {
        return new DashboardTeamMemberDto
        {
            Id = source.Id,
            FullName = source.FullName,
            Initials = source.Initials
        };
    }

    private static EngagementScoreDto ToContract(this EngagementScoreSnapshot source)
    {
        return new EngagementScoreDto
        {
            Score = source.Score,
            Level = source.Level,
            Tip = source.Tip
        };
    }

    private static TeamIndicatorsDto ToContract(this TeamIndicatorsSnapshot source)
    {
        return new TeamIndicatorsDto
        {
            WeeklyAccess = source.WeeklyAccess.ToContract(),
            MissionsUpdated = source.MissionsUpdated.ToContract(),
            FormsResponded = source.FormsResponded.ToContract()
        };
    }

    private static IndicatorDto ToContract(this IndicatorSnapshot source)
    {
        return new IndicatorDto
        {
            Percentage = source.Percentage,
            DeltaPercentage = source.DeltaPercentage,
            IsPlaceholder = source.IsPlaceholder
        };
    }

    private static PendingTaskDto ToContract(this PendingTaskSnapshot source)
    {
        return new PendingTaskDto
        {
            ReferenceId = source.ReferenceId,
            TaskType = source.TaskType,
            Title = source.Title,
            Description = source.Description,
            NavigateUrl = source.NavigateUrl
        };
    }

    public static PagedResult<TDestination> MapPaged<TSource, TDestination>(
        this PagedResult<TSource> source,
        Func<TSource, TDestination> map)
    {
        return new PagedResult<TDestination>
        {
            Items = source.Items.Select(map).ToList(),
            Total = source.Total,
            Page = source.Page,
            PageSize = source.PageSize
        };
    }
}
