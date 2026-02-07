namespace Bud.Server.Data;

public sealed class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public int RetryCount { get; set; }
    public DateTime? NextAttemptOnUtc { get; set; }
    public DateTime? ProcessedOnUtc { get; set; }
    public DateTime? DeadLetteredOnUtc { get; set; }
    public string? Error { get; set; }
}
