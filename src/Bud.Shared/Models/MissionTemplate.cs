namespace Bud.Shared.Models;

public sealed class MissionTemplate : ITenantEntity, IAggregateRoot
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? MissionNamePattern { get; set; }
    public string? MissionDescriptionPattern { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<MissionTemplateMetric> Metrics { get; set; } = new List<MissionTemplateMetric>();

    public static MissionTemplate Create(
        Guid id,
        Guid organizationId,
        string name,
        string? description,
        string? missionNamePattern,
        string? missionDescriptionPattern)
    {
        var template = new MissionTemplate
        {
            Id = id,
            OrganizationId = organizationId,
            IsDefault = false,
            IsActive = true
        };

        template.UpdateBasics(name, description, missionNamePattern, missionDescriptionPattern);
        return template;
    }

    public void UpdateBasics(
        string name,
        string? description,
        string? missionNamePattern,
        string? missionDescriptionPattern)
    {
        if (!EntityName.TryCreate(name, out var entityName))
        {
            throw new DomainInvariantException("O nome do template de missão é obrigatório e deve ter até 200 caracteres.");
        }

        Name = entityName.Value;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        MissionNamePattern = string.IsNullOrWhiteSpace(missionNamePattern) ? null : missionNamePattern.Trim();
        MissionDescriptionPattern = string.IsNullOrWhiteSpace(missionDescriptionPattern) ? null : missionDescriptionPattern.Trim();
    }

    public void SetActive(bool isActive) => IsActive = isActive;

    public void ReplaceMetrics(IEnumerable<MissionTemplateMetricDraft> metricDrafts)
    {
        ArgumentNullException.ThrowIfNull(metricDrafts);

        Metrics = metricDrafts
            .Select(metric => MissionTemplateMetric.Create(
                Guid.NewGuid(),
                OrganizationId,
                Id,
                metric.Name,
                metric.Type,
                metric.OrderIndex,
                metric.QuantitativeType,
                metric.MinValue,
                metric.MaxValue,
                metric.Unit,
                metric.TargetText))
            .ToList();
    }
}
