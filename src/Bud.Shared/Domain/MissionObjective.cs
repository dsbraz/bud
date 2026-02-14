namespace Bud.Shared.Domain;

public sealed class MissionObjective : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public Guid MissionId { get; set; }
    public Mission Mission { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Hierarchy — self-referencing for sub-objectives
    public Guid? ParentObjectiveId { get; set; }
    public MissionObjective? ParentObjective { get; set; }
    public ICollection<MissionObjective> SubObjectives { get; set; } = new List<MissionObjective>();

    public ICollection<MissionMetric> Metrics { get; set; } = new List<MissionMetric>();

    public static MissionObjective Create(
        Guid id,
        Guid organizationId,
        Guid missionId,
        string name,
        string? description)
    {
        if (organizationId == Guid.Empty)
        {
            throw new DomainInvariantException("Objetivo deve pertencer a uma organização válida.");
        }

        if (missionId == Guid.Empty)
        {
            throw new DomainInvariantException("Objetivo deve pertencer a uma missão válida.");
        }

        var objective = new MissionObjective
        {
            Id = id,
            OrganizationId = organizationId,
            MissionId = missionId
        };

        objective.UpdateDetails(name, description);
        return objective;
    }

    public void UpdateDetails(string name, string? description)
    {
        if (!EntityName.TryCreate(name, out var entityName))
        {
            throw new DomainInvariantException(
                "O nome do objetivo é obrigatório e deve ter até 200 caracteres.");
        }

        Name = entityName.Value;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    public void SetParent(Guid? parentObjectiveId)
    {
        ParentObjectiveId = parentObjectiveId;
    }
}
