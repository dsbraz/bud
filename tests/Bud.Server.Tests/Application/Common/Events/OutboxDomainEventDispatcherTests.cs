using Bud.Server.Application.Common.Events;
using Bud.Server.Data;
using Bud.Server.Infrastructure.Events;
using Bud.Server.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.Common.Events;

public sealed class OutboxDomainEventDispatcherTests
{
    [Fact]
    public async Task DispatchAsync_ShouldPersistOutboxMessage()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new ApplicationDbContext(options, new TestTenantProvider { IsGlobalAdmin = true });

        var serializer = new Mock<IOutboxEventSerializer>();
        serializer
            .Setup(s => s.Serialize(It.IsAny<Bud.Server.Domain.Common.Events.IDomainEvent>()))
            .Returns(("test-event", "{\"ok\":true}"));

        var dispatcher = new OutboxDomainEventDispatcher(dbContext, serializer.Object);

        await dispatcher.DispatchAsync(new TestDomainEvent(Guid.NewGuid()));

        dbContext.OutboxMessages.Should().HaveCount(1);
        var message = await dbContext.OutboxMessages.SingleAsync();
        message.EventType.Should().Be("test-event");
        message.Payload.Should().Be("{\"ok\":true}");
        message.RetryCount.Should().Be(0);
        message.NextAttemptOnUtc.Should().NotBeNull();
        message.ProcessedOnUtc.Should().BeNull();
        message.DeadLetteredOnUtc.Should().BeNull();
    }

    private sealed record TestDomainEvent(Guid Id) : Bud.Server.Domain.Common.Events.IDomainEvent;
}
