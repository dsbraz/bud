namespace Bud.Client.Shared.Goals;

public enum WizardMode { Goal, Template }

public enum InlineFormMode { None, NewIndicator, EditIndicator, NewGoal, EditGoal }

public sealed record ScopeOption(string Id, string Name);

public sealed record TempIndicator(
    Guid? OriginalId,
    string Name,
    string Type,
    string Details,
    string? QuantitativeType = null,
    decimal? MinValue = null,
    decimal? MaxValue = null,
    string? TargetText = null,
    string? Unit = null);

public sealed record TempGoal(
    string TempId,
    string Name,
    string? Description,
    Guid? OriginalId = null,
    string? Dimension = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    string? ScopeTypeValue = null,
    string? ScopeId = null,
    string? StatusValue = null)
{
    public List<TempIndicator> Indicators { get; init; } = [];
    public List<TempGoal> Children { get; init; } = [];
}

public sealed record GoalFormModel
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? Dimension { get; init; }
    public DateTime StartDate { get; init; } = DateTime.Today;
    public DateTime EndDate { get; init; } = DateTime.Today.AddDays(7);
    public string? ScopeTypeValue { get; init; }
    public string? ScopeId { get; init; }
    public string? StatusValue { get; init; }
    public List<TempIndicator> Indicators { get; init; } = [];
    public List<TempGoal> Children { get; init; } = [];
}

public sealed record GoalFormResult
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string? Dimension { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public string? ScopeTypeValue { get; init; }
    public string? ScopeId { get; init; }
    public string? StatusValue { get; init; }
    public required List<TempIndicator> Indicators { get; init; }
    public required List<TempGoal> Children { get; init; }
    public HashSet<Guid> DeletedIndicatorIds { get; init; } = [];
    public HashSet<Guid> DeletedGoalIds { get; init; } = [];
}
