using Bud.Shared.Contracts;

namespace Bud.Shared.Contracts.Requests;

public sealed class TemplateMetricRequest
{
    public Guid? TemplateObjectiveId { get; set; }
    public string Name { get; set; } = string.Empty;
    public MetricType Type { get; set; }
    public int OrderIndex { get; set; }
    public QuantitativeMetricType? QuantitativeType { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public MetricUnit? Unit { get; set; }
    public string? TargetText { get; set; }
}

public sealed class TemplateObjectiveRequest
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int OrderIndex { get; set; }
    public string? Dimension { get; set; }
}
