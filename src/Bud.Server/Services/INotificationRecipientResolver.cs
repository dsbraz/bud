namespace Bud.Server.Services;

public interface INotificationRecipientResolver
{
    Task<List<Guid>> ResolveMissionRecipientsAsync(
        Guid missionId,
        Guid organizationId,
        Guid? excludeCollaboratorId = null,
        CancellationToken cancellationToken = default);

    Task<Guid?> ResolveMissionIdFromMetricAsync(
        Guid missionMetricId,
        CancellationToken cancellationToken = default);
}
