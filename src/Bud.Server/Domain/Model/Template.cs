namespace Bud.Server.Domain.Model;

public sealed class Template : ITenantEntity, IAggregateRoot
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? GoalNamePattern { get; set; }
    public string? GoalDescriptionPattern { get; set; }

    public ICollection<TemplateGoal> Goals { get; set; } = new List<TemplateGoal>();
    public ICollection<TemplateIndicator> Indicators { get; set; } = new List<TemplateIndicator>();

    public static Template Create(
        Guid id,
        Guid organizationId,
        string name,
        string? description,
        string? goalNamePattern,
        string? goalDescriptionPattern)
    {
        var template = new Template
        {
            Id = id,
            OrganizationId = organizationId,
        };

        template.UpdateBasics(name, description, goalNamePattern, goalDescriptionPattern);
        return template;
    }

    public void UpdateBasics(
        string name,
        string? description,
        string? goalNamePattern,
        string? goalDescriptionPattern)
    {
        if (!EntityName.TryCreate(name, out var entityName))
        {
            throw new DomainInvariantException("O nome do template de meta é obrigatório e deve ter até 200 caracteres.");
        }

        Name = entityName.Value;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        GoalNamePattern = string.IsNullOrWhiteSpace(goalNamePattern) ? null : goalNamePattern.Trim();
        GoalDescriptionPattern = string.IsNullOrWhiteSpace(goalDescriptionPattern) ? null : goalDescriptionPattern.Trim();
    }

    public void ReplaceIndicators(IEnumerable<TemplateIndicatorDraft> indicatorDrafts)
        => ReplaceGoalsAndIndicators([], indicatorDrafts);

    public void ReplaceGoalsAndIndicators(
        IEnumerable<TemplateGoalDraft> goalDrafts,
        IEnumerable<TemplateIndicatorDraft> indicatorDrafts)
    {
        ArgumentNullException.ThrowIfNull(goalDrafts);
        ArgumentNullException.ThrowIfNull(indicatorDrafts);

        var goalList = goalDrafts
            .Select(goal => TemplateGoal.Create(
                goal.Id ?? Guid.NewGuid(),
                OrganizationId,
                Id,
                goal.Name,
                goal.Description,
                goal.OrderIndex,
                goal.Dimension,
                goal.ParentId))
            .ToList();

        var goalIds = goalList
            .Select(goal => goal.Id)
            .ToHashSet();

        Goals = goalList;

        Indicators = indicatorDrafts
            .Select(indicator =>
            {
                if (indicator.TemplateGoalId.HasValue && !goalIds.Contains(indicator.TemplateGoalId.Value))
                {
                    throw new DomainInvariantException("O indicador referencia uma meta de template inexistente.");
                }

                return TemplateIndicator.Create(
                    Guid.NewGuid(),
                    OrganizationId,
                    Id,
                    indicator.Name,
                    indicator.Type,
                    indicator.OrderIndex,
                    indicator.TemplateGoalId,
                    indicator.QuantitativeType,
                    indicator.MinValue,
                    indicator.MaxValue,
                    indicator.Unit,
                    indicator.TargetText);
            })
            .ToList();
    }
}
