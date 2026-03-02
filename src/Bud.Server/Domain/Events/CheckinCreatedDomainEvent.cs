namespace Bud.Server.Domain.Events;

public sealed record CheckinCreatedDomainEvent(
    Guid CheckinId,
    Guid IndicatorId,
    Guid OrganizationId,
    Guid CollaboratorId) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
