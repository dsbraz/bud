using Bud.Server.Domain.Model;
using FluentAssertions;
using Xunit;

namespace Bud.Server.Tests.Domain.Models;

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
}
