namespace Bud.Shared.Domain;

public sealed class MissionMetric : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public Guid MissionId { get; set; }
    public Mission Mission { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public MetricType Type { get; set; }

    // Quantitative metric fields
    public QuantitativeMetricType? QuantitativeType { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public MetricUnit? Unit { get; set; }

    // Qualitative metric fields
    public string? TargetText { get; set; }

    public ICollection<MetricCheckin> Checkins { get; set; } = [];

    public static MissionMetric Create(Guid id, Guid organizationId, Guid missionId, string name, MetricType type)
    {
        if (organizationId == Guid.Empty)
        {
            throw new DomainInvariantException("Métrica deve pertencer a uma organização válida.");
        }

        if (missionId == Guid.Empty)
        {
            throw new DomainInvariantException("Métrica deve pertencer a uma missão válida.");
        }

        var metric = new MissionMetric
        {
            Id = id,
            OrganizationId = organizationId,
            MissionId = missionId
        };

        metric.UpdateDefinition(name, type);
        return metric;
    }

    public void UpdateDefinition(string name, MetricType type)
    {
        if (!EntityName.TryCreate(name, out var entityName))
        {
            throw new DomainInvariantException("O nome da métrica é obrigatório e deve ter até 200 caracteres.");
        }

        Name = entityName.Value;
        Type = type;
    }

    public void ApplyTarget(
        MetricType type,
        QuantitativeMetricType? quantitativeType,
        decimal? minValue,
        decimal? maxValue,
        MetricUnit? unit,
        string? targetText)
    {
        if (type == MetricType.Qualitative)
        {
            TargetText = string.IsNullOrWhiteSpace(targetText) ? null : targetText.Trim();
            QuantitativeType = null;
            MinValue = null;
            MaxValue = null;
            Unit = null;
            return;
        }

        if (quantitativeType is null)
        {
            throw new DomainInvariantException("Tipo quantitativo é obrigatório para métricas quantitativas.");
        }

        if (minValue.HasValue && maxValue.HasValue)
        {
            _ = MetricRange.Create(minValue, maxValue);
        }

        QuantitativeType = quantitativeType;
        MinValue = minValue;
        MaxValue = maxValue;
        Unit = unit;
        TargetText = null;
    }

    public MetricCheckin CreateCheckin(
        Guid checkinId,
        Guid collaboratorId,
        decimal? value,
        string? text,
        DateTime checkinDate,
        string? note,
        int confidenceLevel)
    {
        ValidateCheckinPayload(value, text);

        return MetricCheckin.Create(
            checkinId,
            OrganizationId,
            Id,
            collaboratorId,
            value,
            text,
            checkinDate,
            note,
            confidenceLevel);
    }

    public void UpdateCheckin(
        MetricCheckin checkin,
        decimal? value,
        string? text,
        DateTime checkinDate,
        string? note,
        int confidenceLevel)
    {
        if (checkin.MissionMetricId != Id)
        {
            throw new DomainInvariantException("Check-in não pertence à métrica informada.");
        }

        ValidateCheckinPayload(value, text);
        checkin.Update(value, text, checkinDate, note, confidenceLevel);
    }

    private void ValidateCheckinPayload(decimal? value, string? text)
    {
        if (Type == MetricType.Quantitative && value is null)
        {
            throw new DomainInvariantException("Valor é obrigatório para métricas quantitativas.");
        }

        if (Type == MetricType.Qualitative && string.IsNullOrWhiteSpace(text))
        {
            throw new DomainInvariantException("Texto é obrigatório para métricas qualitativas.");
        }
    }
}
