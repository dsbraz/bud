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

    public static Notification Create(
        Guid id,
        Guid recipientCollaboratorId,
        Guid organizationId,
        string title,
        string message,
        NotificationType type,
        DateTime createdAtUtc,
        Guid? relatedEntityId = null,
        string? relatedEntityType = null)
    {
        if (recipientCollaboratorId == Guid.Empty)
        {
            throw new DomainInvariantException("A notificação deve ter um destinatário válido.");
        }

        if (organizationId == Guid.Empty)
        {
            throw new DomainInvariantException("A notificação deve pertencer a uma organização válida.");
        }

        if (!NotificationTitle.TryCreate(title, out var normalizedTitle))
        {
            throw new DomainInvariantException("O título da notificação é obrigatório e deve ter até 200 caracteres.");
        }

        if (!NotificationMessage.TryCreate(message, out var normalizedMessage))
        {
            throw new DomainInvariantException("A mensagem da notificação é obrigatória e deve ter até 1000 caracteres.");
        }

        return new Notification
        {
            Id = id,
            RecipientCollaboratorId = recipientCollaboratorId,
            OrganizationId = organizationId,
            Title = normalizedTitle.Value,
            Message = normalizedMessage.Value,
            Type = type,
            IsRead = false,
            CreatedAtUtc = createdAtUtc,
            RelatedEntityId = relatedEntityId,
            RelatedEntityType = string.IsNullOrWhiteSpace(relatedEntityType) ? null : relatedEntityType.Trim()
        };
    }

    public void MarkAsRead(DateTime readAtUtc)
    {
        if (IsRead)
        {
            return;
        }

        IsRead = true;
        ReadAtUtc = readAtUtc;
    }
}
