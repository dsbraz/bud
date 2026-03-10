using Bud.Shared.Kernel;
using System.Text.Json.Serialization;

namespace Bud.Shared.Contracts.Templates;

public sealed class CreateTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? GoalNamePattern { get; set; }
    public string? GoalDescriptionPattern { get; set; }
    public List<TemplateGoalRequest> Goals { get; set; } = [];
    public List<TemplateIndicatorRequest> Indicators { get; set; } = [];
}

public sealed class PatchTemplateRequest
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Optional<string> Name { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Optional<string?> Description { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Optional<string?> GoalNamePattern { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Optional<string?> GoalDescriptionPattern { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Optional<List<TemplateGoalRequest>> Goals { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Optional<List<TemplateIndicatorRequest>> Indicators { get; set; }
}

public sealed class TemplateGoalRequest
{
    public Guid? Id { get; set; }
    public Guid? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int OrderIndex { get; set; }
    public string? Dimension { get; set; }
}

public sealed class TemplateIndicatorRequest
{
    public Guid? TemplateGoalId { get; set; }
    public string Name { get; set; } = string.Empty;
    public IndicatorType Type { get; set; }
    public int OrderIndex { get; set; }
    public QuantitativeIndicatorType? QuantitativeType { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public IndicatorUnit? Unit { get; set; }
    public string? TargetText { get; set; }
}
