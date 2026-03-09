namespace Bud.Application.ReadModels;

public sealed class PendingTaskSnapshot
{
    public Guid ReferenceId { get; set; }
    public string TaskType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string NavigateUrl { get; set; } = string.Empty;
}
