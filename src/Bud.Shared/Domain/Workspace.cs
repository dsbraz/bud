namespace Bud.Shared.Domain;

public sealed class Workspace : ITenantEntity, IAggregateRoot
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public ICollection<Team> Teams { get; set; } = new List<Team>();

    public static Workspace Create(Guid id, Guid organizationId, string name)
    {
        if (organizationId == Guid.Empty)
        {
            throw new DomainInvariantException("Workspace deve pertencer a uma organização válida.");
        }

        var workspace = new Workspace
        {
            Id = id,
            OrganizationId = organizationId
        };

        workspace.Rename(name);
        return workspace;
    }

    public void Rename(string name)
    {
        if (!EntityName.TryCreate(name, out var entityName))
        {
            throw new DomainInvariantException("O nome do workspace é obrigatório e deve ter até 200 caracteres.");
        }

        Name = entityName.Value;
    }
}
