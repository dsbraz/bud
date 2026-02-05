namespace Bud.Shared.Contracts;

public sealed class MissionProgressDto
{
    public Guid MissionId { get; set; }
    public decimal OverallProgress { get; set; }
    public decimal ExpectedProgress { get; set; }
    public decimal AverageConfidence { get; set; }
    public int TotalMetrics { get; set; }
    public int MetricsWithCheckins { get; set; }
}

public sealed class MetricProgressDto
{
    public Guid MetricId { get; set; }
    public decimal Progress { get; set; }
    public int Confidence { get; set; }
    public bool HasCheckins { get; set; }
}
