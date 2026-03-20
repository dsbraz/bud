namespace Bud.Domain.Organizations;

public sealed class Organization : IAggregateRoot
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Plan { get; set; }
    public string? IconUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public static Organization Create(Guid id, string name, string? plan = null, string? iconUrl = null)
    {
        var organization = new Organization
        {
            Id = id,
            Plan = plan,
            IconUrl = iconUrl,
            CreatedAt = DateTime.UtcNow,
        };

        organization.Rename(name);

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

    public void SetPlan(string? plan) => Plan = plan;

    public void SetIconUrl(string? iconUrl) => IconUrl = iconUrl;
}
