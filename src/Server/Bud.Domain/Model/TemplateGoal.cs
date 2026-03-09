namespace Bud.Domain.Model;

public sealed class TemplateGoal : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public Guid TemplateId { get; set; }
    public Template Template { get; set; } = null!;
    public Guid? ParentId { get; set; }
    public TemplateGoal? Parent { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int OrderIndex { get; set; }
    public string? Dimension { get; set; }

    public ICollection<TemplateGoal> Children { get; set; } = [];
    public ICollection<TemplateIndicator> Indicators { get; set; } = [];

    public static TemplateGoal Create(
        Guid id,
        Guid organizationId,
        Guid templateId,
        string name,
        string? description,
        int orderIndex,
        string? dimension,
        Guid? parentId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainInvariantException("O nome da meta do template é obrigatório.");
        }

        if (orderIndex < 0)
        {
            throw new DomainInvariantException("A ordem da meta do template deve ser maior ou igual a zero.");
        }

        if (templateId == Guid.Empty)
        {
            throw new DomainInvariantException("Meta do template deve pertencer a um template válido.");
        }

        return new TemplateGoal
        {
            Id = id,
            OrganizationId = organizationId,
            TemplateId = templateId,
            ParentId = parentId,
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            OrderIndex = orderIndex,
            Dimension = string.IsNullOrWhiteSpace(dimension) ? null : dimension.Trim()
        };
    }
}
