using Bud.BlazorWasm.Services;
using Bud.Shared.Contracts;
using FluentAssertions;
using Xunit;

namespace Bud.BlazorWasm.Tests.Services;

public sealed class EnumParsingHelperTests
{
    [Theory]
    [InlineData("Planned", GoalStatus.Planned)]
    [InlineData("planned", GoalStatus.Planned)]
    [InlineData("ACTIVE", GoalStatus.Active)]
    public void TryParseEnum_WhenValueIsValid_ReturnsTrue(string rawValue, GoalStatus expected)
    {
        var result = EnumParsingHelper.TryParseEnum(rawValue, out GoalStatus parsed);

        result.Should().BeTrue();
        parsed.Should().Be(expected);
    }

    [Fact]
    public void TryParseEnum_WhenValueIsInvalid_ReturnsFalse()
    {
        var result = EnumParsingHelper.TryParseEnum("nao-existe", out GoalStatus parsed);

        result.Should().BeFalse();
        parsed.Should().Be(default);
    }

    [Fact]
    public void TryParseEnum_WhenValueIsNullOrWhiteSpace_ReturnsFalse()
    {
        EnumParsingHelper.TryParseEnum<GoalStatus>(null, out _).Should().BeFalse();
        EnumParsingHelper.TryParseEnum<GoalStatus>(string.Empty, out _).Should().BeFalse();
        EnumParsingHelper.TryParseEnum<GoalStatus>("   ", out _).Should().BeFalse();
    }
}
