using Bud.Shared.Models;
using FluentAssertions;
using Xunit;

namespace Bud.Server.Tests.Domain.Models;

public sealed class ConfidenceLevelTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void TryCreate_WithValidValue_ShouldSucceed(int value)
    {
        var success = ConfidenceLevel.TryCreate(value, out var confidenceLevel);

        success.Should().BeTrue();
        confidenceLevel.Value.Should().Be(value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void TryCreate_WithInvalidValue_ShouldFail(int value)
    {
        var success = ConfidenceLevel.TryCreate(value, out _);

        success.Should().BeFalse();
    }
}
