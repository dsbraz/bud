namespace Bud.Server.Infrastructure.Events;

public sealed class OutboxProcessingOptions
{
    public int MaxRetries { get; init; } = 5;
    public TimeSpan BaseRetryDelay { get; init; } = TimeSpan.FromSeconds(5);
    public TimeSpan MaxRetryDelay { get; init; } = TimeSpan.FromMinutes(5);
    public int BatchSize { get; init; } = 100;
    public TimeSpan PollingInterval { get; init; } = TimeSpan.FromSeconds(5);
}
