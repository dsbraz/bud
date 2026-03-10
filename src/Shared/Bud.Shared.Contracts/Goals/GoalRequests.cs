using Bud.Shared.Kernel;
using System.Text.Json.Serialization;

namespace Bud.Shared.Contracts.Goals;

public sealed class CreateGoalRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Dimension { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public GoalStatus Status { get; set; }
    public Guid? CollaboratorId { get; set; }
    public Guid? ParentId { get; set; }
}

public sealed class PatchGoalRequest
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Optional<string> Name { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Optional<string?> Description { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Optional<string?> Dimension { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Optional<DateTime> StartDate { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Optional<DateTime> EndDate { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Optional<GoalStatus> Status { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Optional<Guid?> CollaboratorId { get; set; }
}
