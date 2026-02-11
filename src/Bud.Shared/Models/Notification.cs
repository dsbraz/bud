namespace Bud.Shared.Models;

public sealed class Notification : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid RecipientCollaboratorId { get; set; }
    public Collaborator RecipientCollaborator { get; set; } = null!;
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ReadAtUtc { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }
}
