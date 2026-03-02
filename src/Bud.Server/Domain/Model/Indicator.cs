using System.ComponentModel.DataAnnotations.Schema;
using Bud.Server.Domain.Events;

namespace Bud.Server.Domain.Model;

public sealed class Indicator : ITenantEntity, IAggregateRoot, IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public Guid GoalId { get; set; }
    public Goal Goal { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public IndicatorType Type { get; set; }

    // Quantitative indicator fields
    public QuantitativeIndicatorType? QuantitativeType { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public IndicatorUnit? Unit { get; set; }

    // Qualitative indicator fields
    public string? TargetText { get; set; }

    public ICollection<Checkin> Checkins { get; set; } = [];

    [NotMapped]
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public static Indicator Create(Guid id, Guid organizationId, Guid goalId, string name, IndicatorType type)
    {
        if (organizationId == Guid.Empty)
        {
            throw new DomainInvariantException("Indicador deve pertencer a uma organização válida.");
        }

        if (goalId == Guid.Empty)
        {
            throw new DomainInvariantException("Indicador deve pertencer a uma meta válida.");
        }

        var indicator = new Indicator
        {
            Id = id,
            OrganizationId = organizationId,
            GoalId = goalId
        };

        indicator.UpdateDefinition(name, type);
        return indicator;
    }

    public void UpdateDefinition(string name, IndicatorType type)
    {
        if (!EntityName.TryCreate(name, out var entityName))
        {
            throw new DomainInvariantException("O nome do indicador é obrigatório e deve ter até 200 caracteres.");
        }

        Name = entityName.Value;
        Type = type;
    }

    public void ApplyTarget(
        IndicatorType type,
        QuantitativeIndicatorType? quantitativeType,
        decimal? minValue,
        decimal? maxValue,
        IndicatorUnit? unit,
        string? targetText)
    {
        if (type == IndicatorType.Qualitative)
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
            throw new DomainInvariantException("Tipo quantitativo é obrigatório para indicadores quantitativos.");
        }

        if (minValue.HasValue && maxValue.HasValue)
        {
            _ = IndicatorRange.Create(minValue, maxValue);
        }

        QuantitativeType = quantitativeType;
        MinValue = minValue;
        MaxValue = maxValue;
        Unit = unit;
        TargetText = null;
    }

    public Checkin CreateCheckin(
        Guid checkinId,
        Guid collaboratorId,
        decimal? value,
        string? text,
        DateTime checkinDate,
        string? note,
        int confidenceLevel)
    {
        ValidateCheckinPayload(value, text);

        var checkin = Checkin.Create(
            checkinId,
            OrganizationId,
            Id,
            collaboratorId,
            value,
            text,
            checkinDate,
            note,
            confidenceLevel);

        AddDomainEvent(new CheckinCreatedDomainEvent(
            checkin.Id,
            Id,
            OrganizationId,
            collaboratorId));

        return checkin;
    }

    public void UpdateCheckin(
        Checkin checkin,
        decimal? value,
        string? text,
        DateTime checkinDate,
        string? note,
        int confidenceLevel)
    {
        if (checkin.IndicatorId != Id)
        {
            throw new DomainInvariantException("Check-in não pertence ao indicador informado.");
        }

        ValidateCheckinPayload(value, text);
        checkin.Update(value, text, checkinDate, note, confidenceLevel);
    }

    private void ValidateCheckinPayload(decimal? value, string? text)
    {
        if (Type == IndicatorType.Quantitative && value is null)
        {
            throw new DomainInvariantException("Valor é obrigatório para indicadores quantitativos.");
        }

        if (Type == IndicatorType.Qualitative && string.IsNullOrWhiteSpace(text))
        {
            throw new DomainInvariantException("Texto é obrigatório para indicadores qualitativos.");
        }
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    private void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
}
