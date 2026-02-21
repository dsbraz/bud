namespace Bud.Server.Domain.Model;

public sealed class MetricCheckin : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public Guid MissionMetricId { get; set; }
    public MissionMetric MissionMetric { get; set; } = null!;
    public Guid CollaboratorId { get; set; }
    public Collaborator Collaborator { get; set; } = null!;

    public decimal? Value { get; set; }
    public string? Text { get; set; }

    public DateTime CheckinDate { get; set; }
    public string? Note { get; set; }
    public int ConfidenceLevel { get; set; }

    public static MetricCheckin Create(
        Guid id,
        Guid organizationId,
        Guid missionMetricId,
        Guid collaboratorId,
        decimal? value,
        string? text,
        DateTime checkinDate,
        string? note,
        int confidenceLevel)
    {
        if (organizationId == Guid.Empty)
        {
            throw new DomainInvariantException("Check-in deve pertencer a uma organização válida.");
        }

        if (missionMetricId == Guid.Empty)
        {
            throw new DomainInvariantException("Check-in deve pertencer a uma métrica válida.");
        }

        if (collaboratorId == Guid.Empty)
        {
            throw new DomainInvariantException("Check-in deve ter um colaborador válido.");
        }

        var checkin = new MetricCheckin
        {
            Id = id,
            OrganizationId = organizationId,
            MissionMetricId = missionMetricId,
            CollaboratorId = collaboratorId
        };

        checkin.Update(value, text, checkinDate, note, confidenceLevel);
        return checkin;
    }

    public void Update(decimal? value, string? text, DateTime checkinDate, string? note, int confidenceLevel)
    {
        var normalizedConfidence = Bud.Server.Domain.Model.ConfidenceLevel.Create(confidenceLevel);

        Value = value;
        Text = string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        CheckinDate = checkinDate;
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        ConfidenceLevel = normalizedConfidence.Value;
    }
}
