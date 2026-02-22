using Bud.Server.Domain.ReadModels;
using Bud.Server.Domain.Model;

namespace Bud.Server.Application.Mapping;

internal static class NotificationsContractMapper
{
    public static NotificationResponse ToResponse(this NotificationSummary source)
    {
        return new NotificationResponse
        {
            Id = source.Id,
            Title = source.Title,
            Message = source.Message,
            Type = source.Type,
            IsRead = source.IsRead,
            CreatedAtUtc = source.CreatedAtUtc,
            ReadAtUtc = source.ReadAtUtc,
            RelatedEntityId = source.RelatedEntityId,
            RelatedEntityType = source.RelatedEntityType
        };
    }

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
