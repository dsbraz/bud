using FluentAssertions;
using Xunit;

namespace Bud.Domain.UnitTests.Domain.Models;

public sealed class IndicatorRangeTests
{
    [Fact]
    public void TryCreate_WithValidRange_ShouldSucceed()
    {
        var success = IndicatorRange.TryCreate(10m, 20m, out var range);

        success.Should().BeTrue();
        range.MinValue.Should().Be(10m);
        range.MaxValue.Should().Be(20m);
    }

    [Fact]
    public void TryCreate_WithInvalidRange_ShouldFail()
    {
        IndicatorRange.TryCreate(null, 20m, out _).Should().BeFalse();
        IndicatorRange.TryCreate(10m, null, out _).Should().BeFalse();
        IndicatorRange.TryCreate(20m, 20m, out _).Should().BeFalse();
        IndicatorRange.TryCreate(30m, 20m, out _).Should().BeFalse();
    }
}
