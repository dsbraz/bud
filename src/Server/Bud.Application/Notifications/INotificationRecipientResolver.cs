namespace Bud.Application.Notifications;

public interface INotificationRecipientResolver
{
    Task<List<Guid>> ResolveGoalRecipientsAsync(
        Guid goalId,
        Guid organizationId,
        Guid? excludeCollaboratorId = null,
        CancellationToken cancellationToken = default);

    Task<Guid?> ResolveGoalIdFromIndicatorAsync(
        Guid indicatorId,
        CancellationToken cancellationToken = default);

    Task<string?> ResolveCollaboratorNameAsync(
        Guid collaboratorId,
        CancellationToken cancellationToken = default);
}
