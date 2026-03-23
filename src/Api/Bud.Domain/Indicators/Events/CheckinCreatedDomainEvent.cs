namespace Bud.Domain.Indicators.Events;

public sealed record CheckinCreatedDomainEvent(
    Guid CheckinId,
    Guid IndicatorId,
    Guid OrganizationId,
    Guid CollaboratorId,
    string IndicatorName) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
