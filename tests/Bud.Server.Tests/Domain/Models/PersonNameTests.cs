using Bud.Shared.Domain;
using FluentAssertions;
using Xunit;

namespace Bud.Server.Tests.Domain.Models;

public sealed class PersonNameTests
{
    [Fact]
    public void TryCreate_WithValidName_ShouldNormalizeSpaces()
    {
        var success = PersonName.TryCreate("  Ana   Clara  Souza  ", out var name);

        success.Should().BeTrue();
        name.Value.Should().Be("Ana Clara Souza");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("A")]
    public void TryCreate_WithInvalidName_ShouldFail(string raw)
    {
        var success = PersonName.TryCreate(raw, out _);

        success.Should().BeFalse();
    }
}
