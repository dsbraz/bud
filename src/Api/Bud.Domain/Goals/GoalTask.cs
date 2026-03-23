
namespace Bud.Domain.Goals;

public sealed class GoalTask : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public Guid GoalId { get; set; }
    public Goal Goal { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskState State { get; set; }
    public DateTime? DueDate { get; private set; }

    public static GoalTask Create(
        Guid id,
        Guid organizationId,
        Guid goalId,
        string name,
        string? description,
        TaskState state,
        DateTime? dueDate = null)
    {
        if (organizationId == Guid.Empty)
        {
            throw new DomainInvariantException("Tarefa deve pertencer a uma organização válida.");
        }

        if (goalId == Guid.Empty)
        {
            throw new DomainInvariantException("Tarefa deve pertencer a uma meta válida.");
        }

        var task = new GoalTask
        {
            Id = id,
            OrganizationId = organizationId,
            GoalId = goalId
        };
        task.UpdateDetails(name, description, state, dueDate);
        return task;
    }

    public void UpdateDetails(string name, string? description, TaskState state, DateTime? dueDate)
    {
        if (!EntityName.TryCreate(name, out var entityName))
        {
            throw new DomainInvariantException("O nome da tarefa é obrigatório e deve ter até 200 caracteres.");
        }

        if (!Enum.IsDefined(state))
        {
            throw new DomainInvariantException("Estado da tarefa inválido.");
        }

        Name = entityName.Value;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        State = state;
        DueDate = dueDate;
    }
}
