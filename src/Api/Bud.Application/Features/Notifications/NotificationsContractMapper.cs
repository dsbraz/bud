
namespace Bud.Application.Features.Notifications;

internal static class NotificationsContractMapper
{
    public static NotificationResponse ToResponse(this Notification source)
    {
        return new NotificationResponse
        {
            Id = source.Id,
            Title = source.Title,
            Message = source.Message,
            Type = source.Type.ToString(),
            IsRead = source.IsRead,
            CreatedAtUtc = source.CreatedAtUtc,
            ReadAtUtc = source.ReadAtUtc,
            RelatedEntityId = source.RelatedEntityId,
            RelatedEntityType = source.RelatedEntityType
        };
    }
}
