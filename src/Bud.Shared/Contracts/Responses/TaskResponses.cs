namespace Bud.Shared.Contracts.Responses;

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
