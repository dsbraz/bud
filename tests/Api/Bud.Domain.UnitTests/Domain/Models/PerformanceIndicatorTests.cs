using FluentAssertions;
using Xunit;

namespace Bud.Domain.UnitTests.Domain.Models;

public sealed class PerformanceIndicatorTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(50, -10)]
    [InlineData(100, 100)]
    [InlineData(100, -100)]
    public void Create_WithValidValues_ShouldSucceed(int percentage, int delta)
    {
        var indicator = PerformanceIndicator.Create(percentage, delta);

        indicator.Percentage.Should().Be(percentage);
        indicator.DeltaPercentage.Should().Be(delta);
        indicator.IsPlaceholder.Should().BeFalse();
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(101, 0)]
    public void Create_WithInvalidPercentage_ShouldThrow(int percentage, int delta)
    {
        var act = () => PerformanceIndicator.Create(percentage, delta);

        act.Should().Throw<DomainInvariantException>();
    }

    [Theory]
    [InlineData(50, -101)]
    [InlineData(50, 101)]
    public void Create_WithInvalidDelta_ShouldThrow(int percentage, int delta)
    {
        var act = () => PerformanceIndicator.Create(percentage, delta);

        act.Should().Throw<DomainInvariantException>();
    }

    [Fact]
    public void Create_WithIsPlaceholder_ShouldSetFlag()
    {
        var indicator = PerformanceIndicator.Create(0, 0, isPlaceholder: true);

        indicator.IsPlaceholder.Should().BeTrue();
    }

    [Fact]
    public void Zero_ShouldReturnZeroValues()
    {
        var indicator = PerformanceIndicator.Zero();

        indicator.Percentage.Should().Be(0);
        indicator.DeltaPercentage.Should().Be(0);
        indicator.IsPlaceholder.Should().BeFalse();
    }

    [Fact]
    public void Placeholder_ShouldReturnPlaceholder()
    {
        var indicator = PerformanceIndicator.Placeholder();

        indicator.Percentage.Should().Be(0);
        indicator.DeltaPercentage.Should().Be(0);
        indicator.IsPlaceholder.Should().BeTrue();
    }
}
