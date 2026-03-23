using FluentAssertions;
using Xunit;

namespace Bud.Domain.UnitTests.Domain.Models;

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

    [Fact]
    public void Create_WithValidName_ShouldSucceed()
    {
        var name = PersonName.Create("João Silva");

        name.Value.Should().Be("João Silva");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("A")]
    public void Create_WithInvalidName_ShouldThrow(string? raw)
    {
        var act = () => PersonName.Create(raw);

        act.Should().Throw<DomainInvariantException>();
    }
}
