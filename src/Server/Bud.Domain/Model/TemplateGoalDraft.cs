namespace Bud.Domain.Model;

public readonly record struct TemplateGoalDraft(
    Guid? Id,
    Guid? ParentId,
    string Name,
    string? Description,
    int OrderIndex,
    string? Dimension);
