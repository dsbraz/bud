namespace Bud.Shared.Models;

public sealed class MissionMetric
{
    public Guid Id { get; set; }
    public Guid MissionId { get; set; }
    public Mission Mission { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public MetricType Type { get; set; }
    public decimal? TargetValue { get; set; }
    public MetricUnit? Unit { get; set; }
    public string? TargetText { get; set; }
}
