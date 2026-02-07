namespace Bud.Server.Infrastructure.Events;

public sealed class OutboxHealthCheckOptions
{
    public int MaxDeadLetters { get; init; }
    public TimeSpan MaxOldestPendingAge { get; init; } = TimeSpan.FromMinutes(15);
}
