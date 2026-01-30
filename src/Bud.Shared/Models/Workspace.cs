namespace Bud.Shared.Models;

public sealed class Workspace : ITenantEntity, IVisibleEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Visibility Visibility { get; set; }
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public ICollection<Team> Teams { get; set; } = new List<Team>();
}
