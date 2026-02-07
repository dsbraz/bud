namespace Bud.Shared.Contracts;

public sealed class OutboxDeadLetterDto
{
    /// <summary>Identificador da mensagem no outbox.</summary>
    public Guid Id { get; set; }
    /// <summary>Data/hora em que o evento ocorreu.</summary>
    public DateTime OccurredOnUtc { get; set; }
    /// <summary>Tipo do evento serializado.</summary>
    public string EventType { get; set; } = string.Empty;
    /// <summary>Quantidade de tentativas já realizadas.</summary>
    public int RetryCount { get; set; }
    /// <summary>Data/hora em que a mensagem foi marcada como dead-letter.</summary>
    public DateTime? DeadLetteredOnUtc { get; set; }
    /// <summary>Último erro registrado no processamento.</summary>
    public string? Error { get; set; }
}

public sealed class ReprocessDeadLettersRequest
{
    /// <summary>Filtro opcional por tipo de evento.</summary>
    public string? EventType { get; set; }
    /// <summary>Filtro opcional de data inicial para dead-letter.</summary>
    public DateTime? DeadLetteredFromUtc { get; set; }
    /// <summary>Filtro opcional de data final para dead-letter.</summary>
    public DateTime? DeadLetteredToUtc { get; set; }
    /// <summary>Quantidade máxima de itens a reprocessar.</summary>
    public int MaxItems { get; set; } = 100;
}

public sealed class ReprocessDeadLettersResponse
{
    /// <summary>Quantidade de mensagens efetivamente reprocessadas.</summary>
    public int ReprocessedCount { get; set; }
}
