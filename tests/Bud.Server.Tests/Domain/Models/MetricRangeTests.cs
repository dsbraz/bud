using Bud.Server.Domain.Model;
using FluentAssertions;
using Xunit;

namespace Bud.Server.Tests.Domain.Models;

public sealed class MetricRangeTests
{
    [Fact]
    public void TryCreate_WithValidRange_ShouldSucceed()
    {
        var success = MetricRange.TryCreate(10m, 20m, out var range);

        success.Should().BeTrue();
        range.MinValue.Should().Be(10m);
        range.MaxValue.Should().Be(20m);
    }

    [Fact]
    public void TryCreate_WithInvalidRange_ShouldFail()
    {
        MetricRange.TryCreate(null, 20m, out _).Should().BeFalse();
        MetricRange.TryCreate(10m, null, out _).Should().BeFalse();
        MetricRange.TryCreate(20m, 20m, out _).Should().BeFalse();
        MetricRange.TryCreate(30m, 20m, out _).Should().BeFalse();
    }
}
