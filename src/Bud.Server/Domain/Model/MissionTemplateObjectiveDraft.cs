namespace Bud.Server.Domain.Model;

public readonly record struct MissionTemplateObjectiveDraft(
    Guid? Id,
    string Name,
    string? Description,
    int OrderIndex,
    string? Dimension);
