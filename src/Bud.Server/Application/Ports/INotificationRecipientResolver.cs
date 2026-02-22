namespace Bud.Server.Application.Ports;

public interface INotificationRecipientResolver
{
    Task<List<Guid>> ResolveMissionRecipientsAsync(
        Guid missionId,
        Guid organizationId,
        Guid? excludeCollaboratorId = null,
        CancellationToken cancellationToken = default);

    Task<Guid?> ResolveMissionIdFromMetricAsync(
        Guid metricId,
        CancellationToken cancellationToken = default);
}
