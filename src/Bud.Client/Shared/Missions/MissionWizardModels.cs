using Bud.Shared.Domain;

namespace Bud.Client.Shared.Missions;

public sealed record ScopeOption(string Id, string Name);

public sealed record TempMetric(
    Guid? OriginalId,
    string Name,
    string Type,
    string Details,
    string? QuantitativeType = null,
    decimal? MinValue = null,
    decimal? MaxValue = null,
    string? TargetText = null,
    string? Unit = null,
    string? ObjectiveTempId = null);

public sealed record TempObjective(
    string TempId,
    string Name,
    string? Description,
    Guid? OriginalId = null,
    Guid? ObjectiveDimensionId = null);

public sealed record MissionWizardModel
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public DateTime StartDate { get; init; } = DateTime.Today;
    public DateTime EndDate { get; init; } = DateTime.Today.AddDays(7);
    public string? ScopeTypeValue { get; init; }
    public string? ScopeId { get; init; }
    public string? StatusValue { get; init; }
    public List<TempMetric> Metrics { get; init; } = [];
    public List<TempObjective> Objectives { get; init; } = [];
}

public sealed record MissionWizardResult
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public string? ScopeTypeValue { get; init; }
    public string? ScopeId { get; init; }
    public string? StatusValue { get; init; }
    public required List<TempMetric> Metrics { get; init; }
    public required List<TempObjective> Objectives { get; init; }
    public HashSet<Guid> DeletedMetricIds { get; init; } = [];
    public HashSet<Guid> DeletedObjectiveIds { get; init; } = [];
}
