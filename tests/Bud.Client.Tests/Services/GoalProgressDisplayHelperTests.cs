using Bud.Client.Services;
using Bud.Shared.Contracts;
using FluentAssertions;
using Xunit;

namespace Bud.Client.Tests.Services;

public sealed class GoalProgressDisplayHelperTests
{
    [Fact]
    public void GetGoalProgressStatusClass_WhenNoCheckins_ShouldReturnNoData()
    {
        var progress = new GoalProgressResponse
        {
            IndicatorsWithCheckins = 0,
            ExpectedProgress = 50,
            OverallProgress = 10
        };

        var result = GoalProgressDisplayHelper.GetGoalProgressStatusClass(progress);

        result.Should().Be("no-data");
    }

    [Fact]
    public void GetIndicatorProgressStatusClass_WhenMediumProgress_ShouldReturnAtRisk()
    {
        var progress = new IndicatorProgressResponse
        {
            HasCheckins = true,
            Progress = 55
        };

        var result = GoalProgressDisplayHelper.GetIndicatorProgressStatusClass(progress);

        result.Should().Be("at-risk");
    }

    [Fact]
    public void GetConfidenceLabel_WhenLow_ShouldReturnBaixa()
    {
        var label = GoalProgressDisplayHelper.GetConfidenceLabel(1.8m);

        label.Should().Be("Baixa");
    }

    [Fact]
    public void GetConfidenceStarsText_WhenIntInput_ShouldClampBetweenZeroAndFive()
    {
        var stars = GoalProgressDisplayHelper.GetConfidenceStarsText(10);

        stars.Should().Be("★★★★★");
    }
}
