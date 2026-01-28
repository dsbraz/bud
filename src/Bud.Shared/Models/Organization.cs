namespace Bud.Shared.Models;

public sealed class Organization
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<Workspace> Workspaces { get; set; } = new List<Workspace>();
}
