using Bud.Shared.Kernel;
using System.Text.Json.Serialization;

namespace Bud.Shared.Contracts.Goals;

public sealed class CreateTaskRequest
{
    public Guid GoalId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskState State { get; set; } = TaskState.ToDo;
    public DateTime? DueDate { get; set; }
}

public sealed class PatchTaskRequest
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Optional<string> Name { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Optional<string?> Description { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Optional<TaskState> State { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Optional<DateTime?> DueDate { get; set; }
}
