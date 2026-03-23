namespace Bud.Application.Features.Indicators;

public sealed class IndicatorProgressSnapshot
{
    public Guid IndicatorId { get; set; }
    public decimal Progress { get; set; }
    public int Confidence { get; set; }
    public bool HasCheckins { get; set; }
    public bool IsOutdated { get; set; }
    public string? LastCheckinCollaboratorName { get; set; }
}
