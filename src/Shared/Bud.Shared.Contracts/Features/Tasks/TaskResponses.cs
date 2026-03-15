using Bud.Shared.Kernel;

namespace Bud.Shared.Contracts.Features.Tasks;

public sealed class TaskResponse
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid GoalId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskState State { get; set; }
    public DateTime? DueDate { get; set; }
}
