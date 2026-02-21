namespace Bud.Server.Domain.Model;

public sealed class Organization : IAggregateRoot
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public Guid? OwnerId { get; set; }
    public Collaborator? Owner { get; set; }

    public ICollection<Workspace> Workspaces { get; set; } = new List<Workspace>();

    public static Organization Create(Guid id, string name, Guid ownerId)
    {
        if (ownerId == Guid.Empty)
        {
            throw new DomainInvariantException("A organização deve possuir um proprietário válido.");
        }

        var organization = new Organization
        {
            Id = id
        };

        organization.Rename(name);
        organization.AssignOwner(ownerId);

        return organization;
    }

    public void Rename(string name)
    {
        if (!EntityName.TryCreate(name, out var entityName))
        {
            throw new DomainInvariantException("O nome da organização é obrigatório e deve ter até 200 caracteres.");
        }

        Name = entityName.Value;
    }

    public void AssignOwner(Guid ownerId)
    {
        if (ownerId == Guid.Empty)
        {
            throw new DomainInvariantException("O proprietário da organização é obrigatório.");
        }

        OwnerId = ownerId;
    }
}
