using Bud.Client.Services;
using Bud.Shared.Contracts;
using FluentAssertions;
using Xunit;

namespace Bud.Client.Tests.Services;

public sealed class MissionProgressDisplayHelperTests
{
    [Fact]
    public void GetMissionProgressStatusClass_WhenNoCheckins_ShouldReturnNoData()
    {
        var progress = new MissionProgressDto
        {
            MetricsWithCheckins = 0,
            ExpectedProgress = 50,
            OverallProgress = 10
        };

        var result = MissionProgressDisplayHelper.GetMissionProgressStatusClass(progress);

        result.Should().Be("no-data");
    }

    [Fact]
    public void GetMetricProgressStatusClass_WhenMediumProgress_ShouldReturnAtRisk()
    {
        var progress = new MetricProgressDto
        {
            HasCheckins = true,
            Progress = 55
        };

        var result = MissionProgressDisplayHelper.GetMetricProgressStatusClass(progress);

        result.Should().Be("at-risk");
    }

    [Fact]
    public void GetConfidenceLabel_WhenLow_ShouldReturnBaixa()
    {
        var label = MissionProgressDisplayHelper.GetConfidenceLabel(1.8m);

        label.Should().Be("Baixa");
    }

    [Fact]
    public void GetConfidenceStarsText_WhenIntInput_ShouldClampBetweenZeroAndFive()
    {
        var stars = MissionProgressDisplayHelper.GetConfidenceStarsText(10);

        stars.Should().Be("★★★★★");
    }
}
