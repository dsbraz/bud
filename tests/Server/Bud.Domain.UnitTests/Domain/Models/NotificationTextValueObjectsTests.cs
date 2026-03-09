using FluentAssertions;
using Xunit;

namespace Bud.Domain.UnitTests.Domain.Models;

public sealed class NotificationTextValueObjectsTests
{
    [Fact]
    public void NotificationTitle_TryCreate_WithValidValue_ShouldSucceed()
    {
        var result = NotificationTitle.TryCreate("  Titulo  ", out var title);

        result.Should().BeTrue();
        title.Value.Should().Be("Titulo");
    }

    [Fact]
    public void NotificationTitle_TryCreate_WithValueLongerThan200_ShouldFail()
    {
        var result = NotificationTitle.TryCreate(new string('A', 201), out _);

        result.Should().BeFalse();
    }

    [Fact]
    public void NotificationTitle_Create_WithValidValue_ShouldSucceed()
    {
        var title = NotificationTitle.Create("Nova missão criada");

        title.Value.Should().Be("Nova missão criada");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void NotificationTitle_Create_WithInvalidValue_ShouldThrow(string? raw)
    {
        var act = () => NotificationTitle.Create(raw);

        act.Should().Throw<DomainInvariantException>();
    }

    [Fact]
    public void NotificationTitle_Create_WithValueTooLong_ShouldThrow()
    {
        var act = () => NotificationTitle.Create(new string('A', 201));

        act.Should().Throw<DomainInvariantException>();
    }

    [Fact]
    public void NotificationMessage_TryCreate_WithValidValue_ShouldSucceed()
    {
        var result = NotificationMessage.TryCreate("  Mensagem  ", out var message);

        result.Should().BeTrue();
        message.Value.Should().Be("Mensagem");
    }

    [Fact]
    public void NotificationMessage_TryCreate_WithValueLongerThan1000_ShouldFail()
    {
        var result = NotificationMessage.TryCreate(new string('B', 1001), out _);

        result.Should().BeFalse();
    }

    [Fact]
    public void NotificationMessage_Create_WithValidValue_ShouldSucceed()
    {
        var message = NotificationMessage.Create("Mensagem de teste");

        message.Value.Should().Be("Mensagem de teste");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void NotificationMessage_Create_WithInvalidValue_ShouldThrow(string? raw)
    {
        var act = () => NotificationMessage.Create(raw);

        act.Should().Throw<DomainInvariantException>();
    }

    [Fact]
    public void NotificationMessage_Create_WithValueTooLong_ShouldThrow()
    {
        var act = () => NotificationMessage.Create(new string('A', 1001));

        act.Should().Throw<DomainInvariantException>();
    }
}
