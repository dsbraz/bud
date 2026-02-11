namespace Bud.Shared.Contracts;

/// <summary>Representação de uma notificação para o cliente.</summary>
public sealed class NotificationDto
{
    /// <summary>Identificador da notificação.</summary>
    public Guid Id { get; set; }

    /// <summary>Título da notificação.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Mensagem detalhada da notificação.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Tipo da notificação (ex: MissionCreated, MetricCheckinCreated).</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Indica se a notificação foi lida.</summary>
    public bool IsRead { get; set; }

    /// <summary>Data de criação da notificação (UTC).</summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Data em que a notificação foi lida (UTC).</summary>
    public DateTime? ReadAtUtc { get; set; }

    /// <summary>Identificador da entidade relacionada à notificação.</summary>
    public Guid? RelatedEntityId { get; set; }

    /// <summary>Tipo da entidade relacionada (ex: Mission, MetricCheckin).</summary>
    public string? RelatedEntityType { get; set; }
}

/// <summary>Resposta com a contagem de notificações não lidas.</summary>
public sealed class UnreadCountResponse
{
    /// <summary>Quantidade de notificações não lidas.</summary>
    public int Count { get; set; }
}
