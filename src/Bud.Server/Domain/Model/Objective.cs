namespace Bud.Server.Domain.Model;

public sealed class Objective : ITenantEntity, IAggregateRoot
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public Guid MissionId { get; set; }
    public Mission Mission { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Dimension { get; set; }

    public ICollection<Metric> Metrics { get; set; } = new List<Metric>();

    public static Objective Create(
        Guid id,
        Guid organizationId,
        Guid missionId,
        string name,
        string? description,
        string? dimension = null)
    {
        if (organizationId == Guid.Empty)
        {
            throw new DomainInvariantException("Objetivo deve pertencer a uma organização válida.");
        }

        if (missionId == Guid.Empty)
        {
            throw new DomainInvariantException("Objetivo deve pertencer a uma missão válida.");
        }

        var objective = new Objective
        {
            Id = id,
            OrganizationId = organizationId,
            MissionId = missionId
        };

        objective.UpdateDetails(name, description, dimension);
        return objective;
    }

    public void UpdateDetails(string name, string? description, string? dimension = null)
    {
        if (!EntityName.TryCreate(name, out var entityName))
        {
            throw new DomainInvariantException(
                "O nome do objetivo é obrigatório e deve ter até 200 caracteres.");
        }

        Name = entityName.Value;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Dimension = string.IsNullOrWhiteSpace(dimension) ? null : dimension.Trim();
    }
}
