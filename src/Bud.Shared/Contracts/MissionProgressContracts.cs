namespace Bud.Shared.Contracts;

public sealed class MissionProgressDto
{
    public Guid MissionId { get; set; }
    public decimal OverallProgress { get; set; }
    public decimal ExpectedProgress { get; set; }
    public decimal AverageConfidence { get; set; }
    public int TotalMetrics { get; set; }
    public int MetricsWithCheckins { get; set; }
    /// <summary>
    /// Metrics with no check-ins or last check-in older than 7 days.
    /// </summary>
    public int OutdatedMetrics { get; set; }
}

public sealed class MetricProgressDto
{
    public Guid MetricId { get; set; }
    public decimal Progress { get; set; }
    public int Confidence { get; set; }
    public bool HasCheckins { get; set; }
    /// <summary>
    /// True if no check-ins or last check-in is older than 7 days.
    /// </summary>
    public bool IsOutdated { get; set; }
}
