using Bud.Server.Data;
using Bud.Server.Services;
using Bud.Server.Tests.Helpers;
using Bud.Shared.Contracts;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bud.Server.Tests.Services;

public sealed class OutboxAdministrationServiceTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options, new TestTenantProvider { IsGlobalAdmin = true });
    }

    [Fact]
    public async Task GetDeadLettersAsync_ShouldReturnOnlyDeadLettersPaged()
    {
        await using var db = CreateContext();

        db.OutboxMessages.Add(new OutboxMessage
        {
            EventType = "A",
            Payload = "{}",
            OccurredOnUtc = DateTime.UtcNow.AddMinutes(-2),
            RetryCount = 5,
            DeadLetteredOnUtc = DateTime.UtcNow.AddMinutes(-1),
            Error = "erro"
        });
        db.OutboxMessages.Add(new OutboxMessage
        {
            EventType = "B",
            Payload = "{}",
            OccurredOnUtc = DateTime.UtcNow,
            RetryCount = 0,
            DeadLetteredOnUtc = null
        });

        await db.SaveChangesAsync();

        var service = new OutboxAdministrationService(db);
        var result = await service.GetDeadLettersAsync(1, 10);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Total.Should().Be(1);
        result.Value.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task ReprocessDeadLetterAsync_WhenFound_ShouldResetState()
    {
        await using var db = CreateContext();

        var message = new OutboxMessage
        {
            EventType = "A",
            Payload = "{}",
            OccurredOnUtc = DateTime.UtcNow,
            RetryCount = 5,
            DeadLetteredOnUtc = DateTime.UtcNow,
            Error = "erro"
        };
        db.OutboxMessages.Add(message);
        await db.SaveChangesAsync();

        var service = new OutboxAdministrationService(db);
        var result = await service.ReprocessDeadLetterAsync(message.Id);

        result.IsSuccess.Should().BeTrue();

        var updated = await db.OutboxMessages.SingleAsync(m => m.Id == message.Id);
        updated.RetryCount.Should().Be(0);
        updated.DeadLetteredOnUtc.Should().BeNull();
        updated.ProcessedOnUtc.Should().BeNull();
        updated.Error.Should().BeNull();
        updated.NextAttemptOnUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task ReprocessDeadLetterAsync_WhenNotFound_ShouldReturnNotFound()
    {
        await using var db = CreateContext();
        var service = new OutboxAdministrationService(db);

        var result = await service.ReprocessDeadLetterAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
    }

    [Fact]
    public async Task ReprocessDeadLetterAsync_WhenMessageIsNotDeadLetter_ShouldReturnValidation()
    {
        await using var db = CreateContext();

        var message = new OutboxMessage
        {
            EventType = "A",
            Payload = "{}",
            OccurredOnUtc = DateTime.UtcNow,
            RetryCount = 0,
            DeadLetteredOnUtc = null
        };
        db.OutboxMessages.Add(message);
        await db.SaveChangesAsync();

        var service = new OutboxAdministrationService(db);
        var result = await service.ReprocessDeadLetterAsync(message.Id);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Validation);
        result.Error.Should().Be("A mensagem informada não está em dead-letter.");
    }

    [Fact]
    public async Task ReprocessDeadLettersAsync_WithFilters_ShouldResetMatchingMessages()
    {
        await using var db = CreateContext();
        var now = DateTime.UtcNow;

        db.OutboxMessages.Add(new OutboxMessage
        {
            EventType = "mission.updated",
            Payload = "{}",
            OccurredOnUtc = now.AddMinutes(-5),
            RetryCount = 5,
            DeadLetteredOnUtc = now.AddMinutes(-2),
            Error = "erro"
        });
        db.OutboxMessages.Add(new OutboxMessage
        {
            EventType = "team.updated",
            Payload = "{}",
            OccurredOnUtc = now.AddMinutes(-5),
            RetryCount = 5,
            DeadLetteredOnUtc = now.AddMinutes(-1),
            Error = "erro"
        });
        await db.SaveChangesAsync();

        var service = new OutboxAdministrationService(db);
        var result = await service.ReprocessDeadLettersAsync(new ReprocessDeadLettersRequest
        {
            EventType = "mission",
            DeadLetteredFromUtc = now.AddMinutes(-3),
            DeadLetteredToUtc = now,
            MaxItems = 50
        });

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);
        db.OutboxMessages.Count(m => m.DeadLetteredOnUtc == null).Should().Be(1);
        db.OutboxMessages.Count(m => m.DeadLetteredOnUtc != null).Should().Be(1);
    }

    [Fact]
    public async Task ReprocessDeadLettersAsync_WithInvalidMaxItems_ShouldReturnValidation()
    {
        await using var db = CreateContext();
        var service = new OutboxAdministrationService(db);

        var result = await service.ReprocessDeadLettersAsync(new ReprocessDeadLettersRequest { MaxItems = 0 });

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Validation);
        result.Error.Should().Be("O parâmetro 'maxItems' deve estar entre 1 e 500.");
    }
}
