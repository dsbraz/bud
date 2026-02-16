namespace Bud.Shared.Domain;

public readonly record struct MissionTemplateObjectiveDraft(
    Guid? Id,
    string Name,
    string? Description,
    int OrderIndex,
    Guid? ObjectiveDimensionId);
