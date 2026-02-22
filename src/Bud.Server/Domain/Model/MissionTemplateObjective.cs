namespace Bud.Server.Domain.Model;

public sealed class MissionTemplateObjective : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public Guid MissionTemplateId { get; set; }
    public MissionTemplate MissionTemplate { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int OrderIndex { get; set; }
    public string? Dimension { get; set; }

    public ICollection<MissionTemplateMetric> Metrics { get; set; } = [];

    public static MissionTemplateObjective Create(
        Guid id,
        Guid organizationId,
        Guid missionTemplateId,
        string name,
        string? description,
        int orderIndex,
        string? dimension)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainInvariantException("O nome do objetivo do template é obrigatório.");
        }

        if (orderIndex < 0)
        {
            throw new DomainInvariantException("A ordem do objetivo do template deve ser maior ou igual a zero.");
        }

        if (missionTemplateId == Guid.Empty)
        {
            throw new DomainInvariantException("Objetivo do template deve pertencer a um template válido.");
        }

        return new MissionTemplateObjective
        {
            Id = id,
            OrganizationId = organizationId,
            MissionTemplateId = missionTemplateId,
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            OrderIndex = orderIndex,
            Dimension = string.IsNullOrWhiteSpace(dimension) ? null : dimension.Trim()
        };
    }
}
