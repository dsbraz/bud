using FluentAssertions;
using Xunit;

namespace Bud.Domain.UnitTests.Domain.Models;

public sealed class EngagementScoreTests
{
    [Theory]
    [InlineData(70, "high")]
    [InlineData(85, "high")]
    [InlineData(100, "high")]
    [InlineData(40, "medium")]
    [InlineData(69, "medium")]
    [InlineData(0, "low")]
    [InlineData(39, "low")]
    public void Create_WithValidScore_ShouldDeriveLevel(int score, string expectedLevel)
    {
        var engagement = EngagementScore.Create(score);

        engagement.Score.Should().Be(score);
        engagement.Level.Should().Be(expectedLevel);
    }

    [Fact]
    public void Create_HighScore_ShouldDeriveHighTip()
    {
        var engagement = EngagementScore.Create(80);

        engagement.Tip.Should().Contain("Excelente");
    }

    [Fact]
    public void Create_MediumScore_ShouldDeriveMediumTip()
    {
        var engagement = EngagementScore.Create(50);

        engagement.Tip.Should().Contain("Bom progresso");
    }

    [Fact]
    public void Create_LowScore_ShouldDeriveLowTip()
    {
        var engagement = EngagementScore.Create(20);

        engagement.Tip.Should().Contain("Atenção");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Create_WithInvalidScore_ShouldThrow(int score)
    {
        var act = () => EngagementScore.Create(score);

        act.Should().Throw<DomainInvariantException>();
    }

    [Fact]
    public void Zero_ShouldReturnLowEngagement()
    {
        var engagement = EngagementScore.Zero();

        engagement.Score.Should().Be(0);
        engagement.Level.Should().Be("low");
        engagement.Tip.Should().Contain("Atenção");
    }
}
