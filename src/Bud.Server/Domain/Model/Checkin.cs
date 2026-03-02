namespace Bud.Server.Domain.Model;

public sealed class Checkin : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public Guid IndicatorId { get; set; }
    public Indicator Indicator { get; set; } = null!;
    public Guid CollaboratorId { get; set; }
    public Collaborator Collaborator { get; set; } = null!;

    public decimal? Value { get; set; }
    public string? Text { get; set; }

    public DateTime CheckinDate { get; set; }
    public string? Note { get; set; }
    public int ConfidenceLevel { get; set; }

    public static Checkin Create(
        Guid id,
        Guid organizationId,
        Guid indicatorId,
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

        if (indicatorId == Guid.Empty)
        {
            throw new DomainInvariantException("Check-in deve pertencer a um indicador válido.");
        }

        if (collaboratorId == Guid.Empty)
        {
            throw new DomainInvariantException("Check-in deve ter um colaborador válido.");
        }

        var checkin = new Checkin
        {
            Id = id,
            OrganizationId = organizationId,
            IndicatorId = indicatorId,
            CollaboratorId = collaboratorId
        };

        checkin.Update(value, text, checkinDate, note, confidenceLevel);
        return checkin;
    }

    public void Update(decimal? value, string? text, DateTime checkinDate, string? note, int confidenceLevel)
    {
        if (confidenceLevel < 1 || confidenceLevel > 5)
        {
            throw new DomainInvariantException("Nível de confiança deve ser entre 1 e 5.");
        }

        Value = value;
        Text = string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        CheckinDate = checkinDate;
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        ConfidenceLevel = confidenceLevel;
    }
}
