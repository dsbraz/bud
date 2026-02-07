using Bud.Server.Data;
using Bud.Server.Infrastructure.Events;
using Bud.Server.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Xunit;

namespace Bud.Server.Tests.Infrastructure.Events;

public sealed class OutboxHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_ShouldReturnHealthy_WhenNoDeadLettersAndNoStalePending()
    {
        var now = new DateTime(2026, 2, 6, 12, 0, 0, DateTimeKind.Utc);
        await using var dbContext = CreateDbContext();
        dbContext.OutboxMessages.Add(new OutboxMessage
        {
            EventType = "evento-teste",
            Payload = "{}",
            OccurredOnUtc = now.AddMinutes(-2),
            NextAttemptOnUtc = now
        });
        await dbContext.SaveChangesAsync();

        var healthCheck = new OutboxHealthCheck(
            dbContext,
            Options.Create(new OutboxHealthCheckOptions
            {
                MaxDeadLetters = 0,
                MaxOldestPendingAge = TimeSpan.FromMinutes(15)
            }),
            () => now);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReturnDegraded_WhenOldestPendingExceedsThreshold()
    {
        var now = new DateTime(2026, 2, 6, 12, 0, 0, DateTimeKind.Utc);
        await using var dbContext = CreateDbContext();
        dbContext.OutboxMessages.Add(new OutboxMessage
        {
            EventType = "evento-teste",
            Payload = "{}",
            OccurredOnUtc = now.AddMinutes(-30),
            NextAttemptOnUtc = now
        });
        await dbContext.SaveChangesAsync();

        var healthCheck = new OutboxHealthCheck(
            dbContext,
            Options.Create(new OutboxHealthCheckOptions
            {
                MaxDeadLetters = 0,
                MaxOldestPendingAge = TimeSpan.FromMinutes(10)
            }),
            () => now);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Degraded);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReturnUnhealthy_WhenDeadLettersExceedThreshold()
    {
        var now = new DateTime(2026, 2, 6, 12, 0, 0, DateTimeKind.Utc);
        await using var dbContext = CreateDbContext();
        dbContext.OutboxMessages.Add(new OutboxMessage
        {
            EventType = "evento-teste",
            Payload = "{}",
            OccurredOnUtc = now.AddMinutes(-30),
            DeadLetteredOnUtc = now.AddMinutes(-5)
        });
        await dbContext.SaveChangesAsync();

        var healthCheck = new OutboxHealthCheck(
            dbContext,
            Options.Create(new OutboxHealthCheckOptions
            {
                MaxDeadLetters = 0,
                MaxOldestPendingAge = TimeSpan.FromMinutes(10)
            }),
            () => now);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options, new TestTenantProvider { IsGlobalAdmin = true });
    }
}
