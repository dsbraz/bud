namespace Bud.Application.Me;

public sealed class DashboardSnapshot
{
    public TeamHealthSnapshot TeamHealth { get; set; } = new();
    public List<PendingTaskSnapshot> PendingTasks { get; set; } = [];
}
