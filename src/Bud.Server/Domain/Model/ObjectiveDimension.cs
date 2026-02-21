namespace Bud.Server.Domain.Model;

public sealed class ObjectiveDimension : ITenantEntity, IAggregateRoot
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public string Name { get; set; } = string.Empty;

    public static ObjectiveDimension Create(Guid id, Guid organizationId, string name)
    {
        var dimension = new ObjectiveDimension
        {
            Id = id,
            OrganizationId = organizationId
        };

        dimension.Rename(name);
        return dimension;
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainInvariantException(
                "O nome da dimensão do objetivo é obrigatório e deve ter até 100 caracteres.");
        }

        var normalizedName = name.Trim();
        if (normalizedName.Length > 100)
        {
            throw new DomainInvariantException(
                "O nome da dimensão do objetivo é obrigatório e deve ter até 100 caracteres.");
        }

        Name = normalizedName;
    }
}
