using Bud.Server.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Bud.Server.Infrastructure.Events;

public sealed class OutboxHealthCheck(
    ApplicationDbContext dbContext,
    IOptions<OutboxHealthCheckOptions> options,
    Func<DateTime>? utcNowProvider = null) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var now = (utcNowProvider ?? (() => DateTime.UtcNow))();
        var deadLetterCount = await dbContext.OutboxMessages
            .CountAsync(m => m.DeadLetteredOnUtc != null && m.ProcessedOnUtc == null, cancellationToken);

        var oldestPending = await dbContext.OutboxMessages
            .Where(m => m.ProcessedOnUtc == null && m.DeadLetteredOnUtc == null)
            .OrderBy(m => m.OccurredOnUtc)
            .Select(m => (DateTime?)m.OccurredOnUtc)
            .FirstOrDefaultAsync(cancellationToken);

        var oldestPendingAge = oldestPending is null
            ? TimeSpan.Zero
            : now - oldestPending.Value;

        var data = new Dictionary<string, object>
        {
            ["deadLetters"] = deadLetterCount,
            ["oldestPendingAgeSeconds"] = Math.Round(oldestPendingAge.TotalSeconds, 2),
            ["maxDeadLetters"] = options.Value.MaxDeadLetters,
            ["maxOldestPendingAgeSeconds"] = Math.Round(options.Value.MaxOldestPendingAge.TotalSeconds, 2)
        };

        if (deadLetterCount > options.Value.MaxDeadLetters)
        {
            return HealthCheckResult.Unhealthy(
                "Outbox possui mensagens em dead-letter acima do limite configurado.",
                data: data);
        }

        if (oldestPendingAge > options.Value.MaxOldestPendingAge)
        {
            return HealthCheckResult.Degraded(
                "Outbox possui mensagens pendentes acima do tempo máximo esperado.",
                data: data);
        }

        return HealthCheckResult.Healthy("Outbox saudável.", data);
    }
}
