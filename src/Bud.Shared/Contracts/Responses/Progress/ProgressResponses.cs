namespace Bud.Shared.Contracts.Responses;

public sealed class MissionProgressResponse
{
    public Guid MissionId { get; set; }
    public decimal OverallProgress { get; set; }
    public decimal ExpectedProgress { get; set; }
    public decimal AverageConfidence { get; set; }
    public int TotalMetrics { get; set; }
    public int MetricsWithCheckins { get; set; }
    public int OutdatedMetrics { get; set; }
    public List<ObjectiveProgressResponse> ObjectiveProgress { get; set; } = [];
}

public sealed class ObjectiveProgressResponse
{
    public Guid ObjectiveId { get; set; }
    public decimal OverallProgress { get; set; }
    public decimal AverageConfidence { get; set; }
    public int TotalMetrics { get; set; }
    public int MetricsWithCheckins { get; set; }
    public int OutdatedMetrics { get; set; }
}

public sealed class MetricProgressResponse
{
    public Guid MetricId { get; set; }
    public decimal Progress { get; set; }
    public int Confidence { get; set; }
    public bool HasCheckins { get; set; }
    public bool IsOutdated { get; set; }
    public string? LastCheckinCollaboratorName { get; set; }
}
