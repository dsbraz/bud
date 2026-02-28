using Bud.Shared.Contracts;

namespace Bud.Shared.Contracts.Responses;

public sealed class GoalResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Dimension { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public GoalStatus Status { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? ParentId { get; set; }
    public Guid? WorkspaceId { get; set; }
    public Guid? TeamId { get; set; }
    public Guid? CollaboratorId { get; set; }
    public WorkspaceResponse? Workspace { get; set; }
    public TeamResponse? Team { get; set; }
    public CollaboratorResponse? Collaborator { get; set; }
    public List<GoalResponse> Children { get; set; } = [];
    public List<IndicatorResponse> Indicators { get; set; } = [];
}

public sealed class GoalProgressResponse
{
    public Guid GoalId { get; set; }
    public decimal OverallProgress { get; set; }
    public decimal ExpectedProgress { get; set; }
    public decimal AverageConfidence { get; set; }
    public int TotalIndicators { get; set; }
    public int IndicatorsWithCheckins { get; set; }
    public int OutdatedIndicators { get; set; }
    public int DirectChildren { get; set; }
    public int DirectIndicators { get; set; }
}
