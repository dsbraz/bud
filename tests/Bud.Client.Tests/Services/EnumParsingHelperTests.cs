using Bud.Client.Services;
using Bud.Shared.Domain;
using FluentAssertions;
using Xunit;

namespace Bud.Client.Tests.Services;

public sealed class EnumParsingHelperTests
{
    [Theory]
    [InlineData("Planned", MissionStatus.Planned)]
    [InlineData("planned", MissionStatus.Planned)]
    [InlineData("ACTIVE", MissionStatus.Active)]
    public void TryParseEnum_WhenValueIsValid_ReturnsTrue(string rawValue, MissionStatus expected)
    {
        var result = EnumParsingHelper.TryParseEnum(rawValue, out MissionStatus parsed);

        result.Should().BeTrue();
        parsed.Should().Be(expected);
    }

    [Fact]
    public void TryParseEnum_WhenValueIsInvalid_ReturnsFalse()
    {
        var result = EnumParsingHelper.TryParseEnum("nao-existe", out MissionStatus parsed);

        result.Should().BeFalse();
        parsed.Should().Be(default);
    }

    [Fact]
    public void TryParseEnum_WhenValueIsNullOrWhiteSpace_ReturnsFalse()
    {
        EnumParsingHelper.TryParseEnum<MissionStatus>(null, out _).Should().BeFalse();
        EnumParsingHelper.TryParseEnum<MissionStatus>(string.Empty, out _).Should().BeFalse();
        EnumParsingHelper.TryParseEnum<MissionStatus>("   ", out _).Should().BeFalse();
    }
}
