using Bud.Server.Application.Common.Events;
using Bud.Server.Data;
using Bud.Server.Infrastructure.Events;
using Bud.Server.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Bud.Server.Tests.Application.Common.Events;

public sealed class OutboxEventProcessorTests
{
    [Fact]
    public async Task ProcessPendingAsync_ShouldInvokeSubscriber_AndMarkProcessed()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new ApplicationDbContext(options, new TestTenantProvider { IsGlobalAdmin = true });
        dbContext.OutboxMessages.Add(new OutboxMessage
        {
            EventType = typeof(TestDomainEvent).AssemblyQualifiedName!,
            Payload = "{}",
            OccurredOnUtc = DateTime.UtcNow,
            NextAttemptOnUtc = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var serializer = new TestOutboxSerializer();
        var subscriber = new TestSubscriber();

        var services = new ServiceCollection();
        services.AddSingleton<IDomainEventSubscriber<TestDomainEvent>>(subscriber);
        using var provider = services.BuildServiceProvider();

        var now = DateTime.UtcNow;
        var processor = new OutboxEventProcessor(
            dbContext,
            provider,
            serializer,
            Options.Create(new OutboxProcessingOptions()),
            () => now);

        await processor.ProcessPendingAsync(50);

        subscriber.HandledCount.Should().Be(1);
        var message = await dbContext.OutboxMessages.SingleAsync();
        message.ProcessedOnUtc.Should().NotBeNull();
        message.DeadLetteredOnUtc.Should().BeNull();
        message.RetryCount.Should().Be(0);
    }

    [Fact]
    public async Task ProcessPendingAsync_WhenSubscriberFails_ShouldScheduleRetryWithBackoff()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new ApplicationDbContext(options, new TestTenantProvider { IsGlobalAdmin = true });
        var now = DateTime.UtcNow;
        dbContext.OutboxMessages.Add(new OutboxMessage
        {
            EventType = typeof(TestDomainEvent).AssemblyQualifiedName!,
            Payload = "{}",
            OccurredOnUtc = now,
            NextAttemptOnUtc = now
        });
        await dbContext.SaveChangesAsync();

        var serializer = new TestOutboxSerializer();
        var subscriber = new ThrowingSubscriber();

        var services = new ServiceCollection();
        services.AddSingleton<IDomainEventSubscriber<TestDomainEvent>>(subscriber);
        using var provider = services.BuildServiceProvider();

        var processor = new OutboxEventProcessor(
            dbContext,
            provider,
            serializer,
            Options.Create(new OutboxProcessingOptions()),
            () => now);
        await processor.ProcessPendingAsync(50);

        var message = await dbContext.OutboxMessages.SingleAsync();
        message.ProcessedOnUtc.Should().BeNull();
        message.DeadLetteredOnUtc.Should().BeNull();
        message.RetryCount.Should().Be(1);
        message.NextAttemptOnUtc.Should().Be(now.AddSeconds(5));
        message.Error.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ProcessPendingAsync_WhenMaxRetriesReached_ShouldMoveToDeadLetter()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new ApplicationDbContext(options, new TestTenantProvider { IsGlobalAdmin = true });
        var now = DateTime.UtcNow;
        dbContext.OutboxMessages.Add(new OutboxMessage
        {
            EventType = typeof(TestDomainEvent).AssemblyQualifiedName!,
            Payload = "{}",
            OccurredOnUtc = now.AddMinutes(-5),
            NextAttemptOnUtc = now,
            RetryCount = 4
        });
        await dbContext.SaveChangesAsync();

        var serializer = new TestOutboxSerializer();
        var subscriber = new ThrowingSubscriber();

        var services = new ServiceCollection();
        services.AddSingleton<IDomainEventSubscriber<TestDomainEvent>>(subscriber);
        using var provider = services.BuildServiceProvider();

        var processor = new OutboxEventProcessor(
            dbContext,
            provider,
            serializer,
            Options.Create(new OutboxProcessingOptions()),
            () => now);
        await processor.ProcessPendingAsync(50);

        var message = await dbContext.OutboxMessages.SingleAsync();
        message.RetryCount.Should().Be(5);
        message.DeadLetteredOnUtc.Should().Be(now);
        message.NextAttemptOnUtc.Should().BeNull();
        message.ProcessedOnUtc.Should().BeNull();
    }

    [Fact]
    public async Task ProcessPendingAsync_ShouldSkipMessagesWithFutureNextAttempt()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new ApplicationDbContext(options, new TestTenantProvider { IsGlobalAdmin = true });
        var now = DateTime.UtcNow;
        dbContext.OutboxMessages.Add(new OutboxMessage
        {
            EventType = typeof(TestDomainEvent).AssemblyQualifiedName!,
            Payload = "{}",
            OccurredOnUtc = now,
            NextAttemptOnUtc = now.AddMinutes(5)
        });
        await dbContext.SaveChangesAsync();

        var serializer = new TestOutboxSerializer();
        var subscriber = new TestSubscriber();

        var services = new ServiceCollection();
        services.AddSingleton<IDomainEventSubscriber<TestDomainEvent>>(subscriber);
        using var provider = services.BuildServiceProvider();

        var processor = new OutboxEventProcessor(
            dbContext,
            provider,
            serializer,
            Options.Create(new OutboxProcessingOptions()),
            () => now);
        var processedCount = await processor.ProcessPendingAsync(50);

        processedCount.Should().Be(0);
        subscriber.HandledCount.Should().Be(0);
        (await dbContext.OutboxMessages.SingleAsync()).ProcessedOnUtc.Should().BeNull();
    }

    private sealed record TestDomainEvent(Guid Id) : Bud.Server.Domain.Common.Events.IDomainEvent;

    private sealed class TestSubscriber : IDomainEventSubscriber<TestDomainEvent>
    {
        public int HandledCount { get; private set; }

        public Task HandleAsync(TestDomainEvent domainEvent, CancellationToken cancellationToken = default)
        {
            HandledCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingSubscriber : IDomainEventSubscriber<TestDomainEvent>
    {
        public Task HandleAsync(TestDomainEvent domainEvent, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("falha de teste");
    }

    private sealed class TestOutboxSerializer : IOutboxEventSerializer
    {
        public (string EventType, string Payload) Serialize(Bud.Server.Domain.Common.Events.IDomainEvent domainEvent)
            => throw new NotSupportedException();

        public Bud.Server.Domain.Common.Events.IDomainEvent Deserialize(string eventType, string payload)
            => new TestDomainEvent(Guid.NewGuid());
    }
}
