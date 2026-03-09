using Bud.Shared.Kernel;

namespace Bud.Shared.Contracts.Responses;

public sealed class TemplateResponse
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? GoalNamePattern { get; set; }
    public string? GoalDescriptionPattern { get; set; }
    public List<TemplateGoalResponse> Goals { get; set; } = [];
    public List<TemplateIndicatorResponse> Indicators { get; set; } = [];
}

public sealed class TemplateGoalResponse
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid TemplateId { get; set; }
    public Guid? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int OrderIndex { get; set; }
    public string? Dimension { get; set; }
    public List<TemplateIndicatorResponse> Indicators { get; set; } = [];
}

public sealed class TemplateIndicatorResponse
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid TemplateId { get; set; }
    public Guid? TemplateGoalId { get; set; }
    public string Name { get; set; } = string.Empty;
    public IndicatorType Type { get; set; }
    public int OrderIndex { get; set; }
    public QuantitativeIndicatorType? QuantitativeType { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public IndicatorUnit? Unit { get; set; }
    public string? TargetText { get; set; }
    public TemplateGoalResponse? Goal { get; set; }
}
